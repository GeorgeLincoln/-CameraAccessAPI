using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext context,
        ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserCameras)
                .ThenInclude(uc => uc.Camera)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Camera)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Days)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Schedules)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserCameras)
                .ThenInclude(uc => uc.Camera)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Camera)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Days)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Schedules)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null
            ? null
            : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(UserInputDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Document = dto.Document,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // =====================================================
        // VÍNCULO DE CÂMERAS
        // =====================================================

        foreach (var cameraId in dto.CameraIds.Distinct())
        {
            var cameraExists = await _context.Cameras
                .AnyAsync(c => c.Id == cameraId);

            if (!cameraExists)
                continue;

            user.UserCameras.Add(new UserCamera
            {
                UserId = user.Id,
                CameraId = cameraId
            });
        }

        // =====================================================
        // REGRAS DE ACESSO
        // =====================================================

        foreach (var rule in dto.Rules)
        {
            if (rule.CameraId.HasValue)
            {
                var cameraExists = await _context.Cameras
                    .AnyAsync(c => c.Id == rule.CameraId.Value);

                if (!cameraExists)
                    continue;
            }

            var accessRule = new AccessRule
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CameraId = rule.CameraId ?? Guid.Empty,
                Allowed = true,
                Active = true
            };

            // =========================
            // DIAS
            // =========================

            foreach (var day in rule.DaysOfWeek.Distinct())
            {
                accessRule.Days.Add(new AccessDay
                {
                    Id = Guid.NewGuid(),
                    Day = (int)day
                });
            }

            // =========================
            // HORÁRIOS
            // =========================

            accessRule.Schedules.Add(new AccessSchedule
            {
                Id = Guid.NewGuid(),
                StartTime = rule.StartTime,
                EndTime = rule.EndTime
            });

            user.AccessRules.Add(accessRule);
        }

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User created. UserId={UserId}",
            user.Id);

        return await GetByIdAsync(user.Id)
            ?? MapToDto(user);
    }

    public async Task<bool> UpdateAsync(Guid id, UserInputDto dto)
    {
        var user = await _context.Users
            .Include(u => u.UserCameras)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Days)
            .Include(u => u.AccessRules)
                .ThenInclude(ar => ar.Schedules)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return false;

        user.Name = dto.Name;
        user.Document = dto.Document;
        user.Active = dto.Active;
        user.UpdatedAt = DateTime.UtcNow;

        // =====================================================
        // ATUALIZAR CÂMERAS
        // =====================================================

        _context.UserCameras.RemoveRange(user.UserCameras);

        foreach (var cameraId in dto.CameraIds.Distinct())
        {
            var cameraExists = await _context.Cameras
                .AnyAsync(c => c.Id == cameraId);

            if (!cameraExists)
                continue;

            user.UserCameras.Add(new UserCamera
            {
                UserId = user.Id,
                CameraId = cameraId
            });
        }

        // =====================================================
        // REMOVER REGRAS ANTIGAS
        // =====================================================

        foreach (var rule in user.AccessRules)
        {
            _context.AccessDays.RemoveRange(rule.Days);
            _context.AccessSchedules.RemoveRange(rule.Schedules);
        }

        _context.AccessRules.RemoveRange(user.AccessRules);

        // =====================================================
        // CRIAR NOVAS REGRAS
        // =====================================================

        foreach (var rule in dto.Rules)
        {
            if (rule.CameraId.HasValue)
            {
                var cameraExists = await _context.Cameras
                    .AnyAsync(c => c.Id == rule.CameraId.Value);

                if (!cameraExists)
                    continue;
            }

            var accessRule = new AccessRule
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CameraId = rule.CameraId ?? Guid.Empty,
                Allowed = true,
                Active = true
            };

            // =========================
            // DIAS
            // =========================

            foreach (var day in rule.DaysOfWeek.Distinct())
            {
                accessRule.Days.Add(new AccessDay
                {
                    Id = Guid.NewGuid(),
                    Day = (int)day
                });
            }

            // =========================
            // HORÁRIOS
            // =========================

            accessRule.Schedules.Add(new AccessSchedule
            {
                Id = Guid.NewGuid(),
                StartTime = rule.StartTime,
                EndTime = rule.EndTime
            });

            user.AccessRules.Add(accessRule);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User updated. UserId={UserId}",
            user.Id);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return false;

        // Soft delete
        user.Active = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User soft deleted. UserId={UserId}",
            user.Id);

        return true;
    }

    public async Task<bool> LinkCameraAsync(Guid userId, Guid cameraId)
    {
        var exists = await _context.UserCameras
            .AnyAsync(uc =>
                uc.UserId == userId &&
                uc.CameraId == cameraId);

        if (exists)
            return true;

        var userExists = await _context.Users
            .AnyAsync(u => u.Id == userId);

        var cameraExists = await _context.Cameras
            .AnyAsync(c => c.Id == cameraId);

        if (!userExists || !cameraExists)
            return false;

        _context.UserCameras.Add(new UserCamera
        {
            UserId = userId,
            CameraId = cameraId
        });

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnlinkCameraAsync(Guid userId, Guid cameraId)
    {
        var entity = await _context.UserCameras
            .FirstOrDefaultAsync(uc =>
                uc.UserId == userId &&
                uc.CameraId == cameraId);

        if (entity == null)
            return false;

        _context.UserCameras.Remove(entity);

        await _context.SaveChangesAsync();

        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Document = user.Document,
            Active = user.Active,

            Cameras = user.UserCameras
                .Where(uc => uc.Camera != null)
                .Select(uc => new CameraSummaryDto
                {
                    Id = uc.Camera.Id,
                    Name = uc.Camera.Name
                })
                .ToList(),

            Rules = user.AccessRules
                .Select(ar => new AccessRuleSummaryDto
                {
                    Id = ar.Id,
                    CameraId = ar.CameraId,
                    CameraName = ar.Camera?.Name,

                    DaysOfWeek = ar.Days
                        .Select(d => (int)d.Day)
                        .ToList(),

                    StartTime = ar.Schedules
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefault()?.StartTime ?? TimeSpan.Zero,

                    EndTime = ar.Schedules
                        .OrderByDescending(s => s.EndTime)
                        .FirstOrDefault()?.EndTime ?? TimeSpan.Zero
                })
                .ToList()
        };
    }
}