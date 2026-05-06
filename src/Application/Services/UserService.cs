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

    public UserService(AppDbContext context, ILogger<UserService> logger)
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
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null ? null : MapToDto(user);
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

        foreach (var cameraId in dto.CameraIds)
        {
            var cameraExists = await _context.Cameras.AnyAsync(c => c.Id == cameraId);
            if (cameraExists)
            {
                user.UserCameras.Add(new UserCamera { UserId = user.Id, CameraId = cameraId });
            }
        }

        foreach (var rule in dto.Rules)
        {
            if (rule.CameraId.HasValue)
            {
                var cameraExists = await _context.Cameras.AnyAsync(c => c.Id == rule.CameraId.Value);
                if (!cameraExists) continue;
            }

            user.AccessRules.Add(new AccessRule
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CameraId = rule.CameraId,
                StartTime = rule.StartTime,
                EndTime = rule.EndTime,
                DaysOfWeek = rule.DaysOfWeek.ToArray(),
                Allowed = true,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(user.Id) ?? MapToDto(user);
    }

    public async Task<bool> UpdateAsync(Guid id, UserInputDto dto)
    {
        var user = await _context.Users
            .Include(u => u.UserCameras)
            .Include(u => u.AccessRules)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return false;

        user.Name = dto.Name;
        user.Document = dto.Document;
        user.Active = dto.Active;
        user.UpdatedAt = DateTime.UtcNow;

        user.UserCameras.Clear();
        foreach (var cameraId in dto.CameraIds)
        {
            var cameraExists = await _context.Cameras.AnyAsync(c => c.Id == cameraId);
            if (cameraExists)
            {
                user.UserCameras.Add(new UserCamera { UserId = user.Id, CameraId = cameraId });
            }
        }

        _context.AccessRules.RemoveRange(user.AccessRules);
        
        foreach (var rule in dto.Rules)
        {
            if (rule.CameraId.HasValue)
            {
                var cameraExists = await _context.Cameras.AnyAsync(c => c.Id == rule.CameraId.Value);
                if (!cameraExists) continue;
            }

            user.AccessRules.Add(new AccessRule
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CameraId = rule.CameraId,
                StartTime = rule.StartTime,
                EndTime = rule.EndTime,
                DaysOfWeek = rule.DaysOfWeek.ToArray(),
                Allowed = true,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        // Soft delete
        user.Active = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LinkCameraAsync(Guid userId, Guid cameraId)
    {
        var exists = await _context.UserCameras.AnyAsync(uc => uc.UserId == userId && uc.CameraId == cameraId);
        if (exists) return true;

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        var cameraExists = await _context.Cameras.AnyAsync(c => c.Id == cameraId);

        if (!userExists || !cameraExists) return false;

        _context.UserCameras.Add(new UserCamera { UserId = userId, CameraId = cameraId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlinkCameraAsync(Guid userId, Guid cameraId)
    {
        var userCamera = await _context.UserCameras.FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CameraId == cameraId);
        if (userCamera == null) return false;

        _context.UserCameras.Remove(userCamera);
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
            Cameras = user.UserCameras.Select(uc => new CameraSummaryDto
            {
                Id = uc.Camera.Id,
                Name = uc.Camera.Name
            }).ToList(),
            Rules = user.AccessRules.Select(ar => new AccessRuleSummaryDto
            {
                Id = ar.Id,
                CameraId = ar.CameraId,
                CameraName = ar.Camera?.Name,
                StartTime = ar.StartTime,
                EndTime = ar.EndTime,
                DaysOfWeek = ar.DaysOfWeek.ToList()
            }).ToList()
        };
    }
}
