using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

public class AccessRuleService : IAccessRuleService
{
    private readonly IAccessRuleRepository _repository;
    private readonly ILogger<AccessRuleService> _logger;

    public AccessRuleService(
        IAccessRuleRepository repository,
        ILogger<AccessRuleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<AccessRuleDto>> GetAllAsync()
    {
        var rules = await _repository.GetAllAsync();
        return rules.Select(MapToDto).ToList();
    }

    public async Task<AccessRuleDto?> GetByIdAsync(Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);
        return rule == null ? null : MapToDto(rule);
    }

    public async Task<AccessRuleDto> CreateAsync(AccessRuleInputDto rule)
    {
        var camera = await _repository.GetCameraByIdAsync(rule.CameraId);

        if (camera == null)
            throw new InvalidOperationException("Camera não encontrada.");

        if (!Guid.TryParse(rule.UserId, out var userIdGuid))
            throw new InvalidOperationException("UserId inválido. Deve ser um GUID.");

        var schedule = rule.Schedules.FirstOrDefault();

        var entity = new AccessRule
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            CameraId = camera.Id,
            Allowed = rule.Allowed,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DaysOfWeek = rule.Days.ToArray(),
            StartTime = schedule?.StartTime ?? TimeSpan.Zero,
            EndTime = schedule?.EndTime ?? new TimeSpan(23, 59, 59)
        };

        await _repository.AddAsync(entity);
        entity.Camera = camera;

        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, AccessRuleInputDto rule)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return false;

        var camera = await _repository.GetCameraByIdAsync(rule.CameraId);
        if (camera == null)
            throw new InvalidOperationException("Camera não encontrada.");

        if (!Guid.TryParse(rule.UserId, out var userIdGuid))
            throw new InvalidOperationException("UserId inválido. Deve ser um GUID.");

        var schedule = rule.Schedules.FirstOrDefault();

        existing.UserId = userIdGuid;
        existing.CameraId = camera.Id;
        existing.Camera = camera;
        existing.Allowed = rule.Allowed;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.DaysOfWeek = rule.Days.ToArray();
        existing.StartTime = schedule?.StartTime ?? TimeSpan.Zero;
        existing.EndTime = schedule?.EndTime ?? new TimeSpan(23, 59, 59);

        await _repository.UpdateAsync(existing);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    private static AccessRuleDto MapToDto(AccessRule rule)
    {
        return new AccessRuleDto
        {
            Id = rule.Id,
            UserId = rule.UserId.ToString(),
            CameraId = rule.CameraId ?? Guid.Empty,
            CameraName = rule.Camera?.Name ?? "Global",
            Allowed = rule.Allowed,
            Days = rule.DaysOfWeek.ToList(),
            Schedules = new List<AccessScheduleDto>
            {
                new AccessScheduleDto
                {
                    StartTime = rule.StartTime,
                    EndTime = rule.EndTime
                }
            },
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
