using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

public class AccessValidationService : IAccessValidationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccessValidationService> _logger;

    public AccessValidationService(AppDbContext context, ILogger<AccessValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccessValidationResponseDto> ValidateAccessAsync(AccessValidationRequestDto request)
    {
        var timestamp = request.Timestamp ?? DateTime.UtcNow;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user == null || !user.Active)
        {
            await LogAccess(request.UserId, request.CameraId, timestamp, false, "Usuário não encontrado ou inativo");
            return new AccessValidationResponseDto { Allowed = false, Reason = "Usuário inativo ou não existe" };
        }

        var camera = await _context.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CameraId);
        if (camera == null || !camera.Active)
        {
            await LogAccess(request.UserId, request.CameraId, timestamp, false, "Câmera não encontrada ou inativa");
            return new AccessValidationResponseDto { Allowed = false, Reason = "Câmera inativa ou não existe" };
        }

        var hasLink = await _context.UserCameras.AnyAsync(uc => uc.UserId == request.UserId && uc.CameraId == request.CameraId);
        if (!hasLink)
        {
            await LogAccess(request.UserId, request.CameraId, timestamp, false, "Usuário não vinculado à câmera");
            return new AccessValidationResponseDto { Allowed = false, Reason = "Usuário não vinculado à câmera" };
        }

       

        var rules = await _context.AccessRules
    .AsNoTracking()
    .Include(r => r.Days)
    .Include(r => r.Schedules)
    .Where(r =>
        r.UserId == request.UserId &&
        r.CameraId == request.CameraId)
    .ToListAsync();

if (!rules.Any())
{
    await LogAccess(
        request.UserId,
        request.CameraId,
        timestamp,
        false,
        "Nenhuma regra encontrada");

    return new AccessValidationResponseDto
    {
        Allowed = false,
        Reason = "Sem regras de acesso"
    };
}

var currentDay = timestamp.DayOfWeek;
var currentTime = timestamp.TimeOfDay;

foreach (var rule in rules.Where(r => r.Active && r.Allowed))
{
    var validDay = rule.Days
        .Any(d => d.Day == (int)currentDay);

    if (!validDay)
        continue;

    var validTime = rule.Schedules
        .Any(schedule =>
            currentTime >= schedule.StartTime &&
            currentTime <= schedule.EndTime);

    if (!validTime)
        continue;

    await LogAccess(
        request.UserId,
        request.CameraId,
        timestamp,
        true,
        "Acesso permitido");

    return new AccessValidationResponseDto
    {
        Allowed = true,
        Reason = "Acesso permitido"
    };
}

        await LogAccess(request.UserId, request.CameraId, timestamp, false, "Fora do horário permitido");
        return new AccessValidationResponseDto { Allowed = false, Reason = "Acesso negado: Fora do horário permitido" };
    }

    private async Task LogAccess(Guid userId, Guid cameraId, DateTime timestamp, bool allowed, string reason)
    {
        try
        {
            var log = new AccessLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CameraId = cameraId,
                Timestamp = timestamp,
                Allowed = allowed,
                Reason = reason,
                Source = "API_Validate"
            };

            _context.AccessLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar AccessLog");
        }
    }
}
