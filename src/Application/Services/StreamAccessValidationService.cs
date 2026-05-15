using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

/// <summary>
/// Serviço responsável por validar acesso a streams protegidas.
/// 
/// Fluxo:
/// 1. Valida JWT
/// 2. Valida usuário
/// 3. Resolve stream -> câmera
/// 4. Valida câmera
/// 5. Valida vínculo usuário-câmera
/// 6. Valida regras de acesso
/// 7. Registra auditoria
/// </summary>
public class StreamAccessValidationService : IStreamAccessValidationService
{
    private readonly IStreamTokenService _tokenService;
    private readonly AppDbContext _context;
    private readonly ILogger<StreamAccessValidationService> _logger;
    private readonly IConfiguration _configuration;

    public StreamAccessValidationService(
        IStreamTokenService tokenService,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<StreamAccessValidationService> logger)
    {
        _tokenService = tokenService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StreamTokenValidationResponseDto> ValidateStreamAccessAsync(
        StreamTokenValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var response = new StreamTokenValidationResponseDto
        {
            Allowed = false,
            ProcessedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Starting stream validation. Stream={Stream}, ClientIp={ClientIp}",
            request.StreamName,
            request.ClientIp);

        // =========================================================
        // PASSO 1 - VALIDAR TOKEN
        // =========================================================

        var claims = await _tokenService.ValidateAndExtractClaimsAsync(
            request.Token,
            cancellationToken);

        if (claims == null)
        {
            response.Reason = "Invalid token";

            _logger.LogWarning(
                "Access denied. Invalid token. Stream={Stream}",
                request.StreamName);

            return response;
        }

        response.UserId = claims.UserId;

        // =========================================================
        // PASSO 2 - VALIDAR STREAM DO TOKEN
        // =========================================================

        if (!string.Equals(
                claims.StreamName?.Trim(),
                request.StreamName?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            response.Reason = "Token stream mismatch";

            await LogAccessAsync(
                claims.UserId,
                null,
                false,
                "Token stream mismatch",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        // =========================================================
        // PASSO 3 - VALIDAR USUÁRIO
        // =========================================================

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == claims.UserId,
                cancellationToken);

        if (user == null)
        {
            response.Reason = "User not found";

            await LogAccessAsync(
                claims.UserId,
                null,
                false,
                "User not found",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        if (!user.Active)
        {
            response.Reason = "Inactive user";

            await LogAccessAsync(
                claims.UserId,
                null,
                false,
                "Inactive user",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        // =========================================================
        // PASSO 4 - RESOLVER STREAM -> CAMERA
        // =========================================================

        var cameraId = await ResolveStreamNameToCameraIdAsync(
            request.StreamName ?? string.Empty,
            cancellationToken);

        if (cameraId == null)
        {
            response.Reason = "Camera not found";

            await LogAccessAsync(
                claims.UserId,
                null,
                false,
                "Camera not found",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        response.CameraId = cameraId.Value;

        // =========================================================
        // PASSO 5 - VALIDAR CAMERA
        // =========================================================

        var camera = await _context.Cameras
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == cameraId.Value,
                cancellationToken);

        if (camera == null)
        {
            response.Reason = "Camera not found";

            await LogAccessAsync(
                claims.UserId,
                cameraId,
                false,
                "Camera not found",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        // Ajuste conforme sua entidade Camera atual
        if (!camera.Active)
        {
            response.Reason = "Inactive camera";

            await LogAccessAsync(
                claims.UserId,
                cameraId,
                false,
                "Inactive camera",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        // =========================================================
        // PASSO 6 - VALIDAR VÍNCULO USUÁRIO-CÂMERA
        // =========================================================

        var hasLink = await _context.UserCameras
            .AsNoTracking()
            .AnyAsync(
                uc =>
                    uc.UserId == claims.UserId &&
                    uc.CameraId == cameraId.Value,
                cancellationToken);

        if (!hasLink)
        {
            response.Reason = "User not linked to camera";

            await LogAccessAsync(
                claims.UserId,
                cameraId,
                false,
                "User not linked to camera",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        // =========================================================
        // PASSO 7 - VALIDAR REGRAS
        // =========================================================

        var businessNow = ResolveBusinessNowUtcAware();

        var currentDay = businessNow.DayOfWeek;
        var currentTime = businessNow.TimeOfDay;

        var rules = await _context.AccessRules
            .AsNoTracking()
            .Include(r => r.Days)
            .Include(r => r.Schedules)
            .Where(r =>
                r.UserId == claims.UserId &&
                r.CameraId == cameraId.Value &&
                r.Active &&
                r.Allowed)
            .ToListAsync(cancellationToken);

        if (!rules.Any())
        {
            response.Reason = "No access rules";

            await LogAccessAsync(
                claims.UserId,
                cameraId,
                false,
                "No access rules",
                request.ClientIp,
                cancellationToken);

            return response;
        }

        foreach (var rule in rules)
        {
            // ==========================================
            // VALIDAR DIA
            // ==========================================

            var hasValidDay =
                !rule.Days.Any() ||
                rule.Days.Any(d => d.Day == (int)currentDay);

            if (!hasValidDay)
                continue;

            // ==========================================
            // VALIDAR HORÁRIO
            // ==========================================

            foreach (var schedule in rule.Schedules)
            {
                var validTime = IsWithinTimeWindow(
                    currentTime,
                    schedule.StartTime,
                    schedule.EndTime);

                if (!validTime)
                    continue;

                // ==========================================
                // ACESSO LIBERADO
                // ==========================================

                response.Allowed = true;
                response.Reason = "Access granted";

                await LogAccessAsync(
                    claims.UserId,
                    cameraId,
                    true,
                    "Access granted",
                    request.ClientIp,
                    cancellationToken);

                _logger.LogInformation(
                    "Access granted. UserId={UserId}, CameraId={CameraId}",
                    claims.UserId,
                    cameraId);

                return response;
            }
        }

        // =========================================================
        // ACESSO NEGADO
        // =========================================================

        response.Reason = "Outside allowed schedule";

        await LogAccessAsync(
            claims.UserId,
            cameraId,
            false,
            "Outside allowed schedule",
            request.ClientIp,
            cancellationToken);

        return response;
    }

    /// <summary>
    /// Resolve nome da stream para ID da câmera.
    /// </summary>
    public async Task<Guid?> ResolveStreamNameToCameraIdAsync(
        string streamName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            return null;

        var camera = await _context.Cameras
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Name == streamName,
                cancellationToken);

        return camera?.Id;
    }

    /// <summary>
    /// Registra auditoria de acesso.
    /// </summary>
    private async Task LogAccessAsync(
        Guid userId,
        Guid? cameraId,
        bool allowed,
        string reason,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AccessLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CameraId = cameraId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow,
                Allowed = allowed,
                Reason = reason,
                Source = $"MediaMTX_{clientIp}"
            };

            _context.AccessLogs.Add(log);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error registering access log");
        }
    }

    /// <summary>
    /// Resolve timezone configurado.
    /// </summary>
    private DateTime ResolveBusinessNowUtcAware()
    {
        var timezoneId = _configuration["AccessControl:Timezone"];

        if (string.IsNullOrWhiteSpace(timezoneId))
            return DateTime.UtcNow;

        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                timezone);
        }
        catch
        {
            _logger.LogWarning(
                "Invalid timezone configuration. Using UTC.");

            return DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Valida janela de horário.
    /// </summary>
    private static bool IsWithinTimeWindow(
        TimeSpan current,
        TimeSpan start,
        TimeSpan end)
    {
        // 00:00 -> 00:00 = acesso 24h
        if (start == end)
            return true;

        // Exemplo: 08:00 -> 18:00
        if (start < end)
        {
            return current >= start &&
                   current <= end;
        }

        // Overnight
        // Exemplo: 22:00 -> 02:00
        return current >= start ||
               current <= end;
    }
}