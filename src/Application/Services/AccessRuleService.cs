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

        var entity = new AccessRule
        {
            Id = Guid.NewGuid(),
            UserId = rule.UserId,
            CameraId = camera.Id,
            Allowed = rule.Allowed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Days = rule.Days.Select(day => new AccessDay { DayOfWeek = day }).ToList(),
            Schedules = rule.Schedules.Select(schedule => new AccessSchedule
            {
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime
            }).ToList()
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

        existing.UserId = rule.UserId;
        existing.CameraId = camera.Id;
        existing.Camera = camera;
        existing.Allowed = rule.Allowed;
        existing.UpdatedAt = DateTime.UtcNow;

        existing.Days.Clear();
        foreach (var day in rule.Days)
        {
            existing.Days.Add(new AccessDay { DayOfWeek = day, AccessRuleId = existing.Id });
        }

        existing.Schedules.Clear();
        foreach (var schedule in rule.Schedules)
        {
            existing.Schedules.Add(new AccessSchedule
            {
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                AccessRuleId = existing.Id
            });
        }

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
            UserId = rule.UserId,
            CameraId = rule.CameraId,
            CameraName = rule.Camera.Name,
            Allowed = rule.Allowed,
            Days = rule.Days.Select(d => d.DayOfWeek).ToList(),
            Schedules = rule.Schedules.Select(s => new AccessScheduleDto
            {
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList(),
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
