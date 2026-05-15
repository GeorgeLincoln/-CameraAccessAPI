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

        return rules
            .Select(MapToDto)
            .ToList();
    }

    public async Task<AccessRuleDto?> GetByIdAsync(Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);

        return rule == null
            ? null
            : MapToDto(rule);
    }

    public async Task<AccessRuleDto> CreateAsync(AccessRuleInputDto rule)
    {
        ValidateRuleInput(rule);

        var camera = await _repository.GetCameraByIdAsync(rule.CameraId);

        if (camera == null)
            throw new InvalidOperationException("Camera não encontrada.");

        if (!Guid.TryParse(rule.UserId, out var userId))
            throw new InvalidOperationException("UserId inválido.");

        var entity = new AccessRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CameraId = camera.Id,
            Camera = camera,
            Active = true,
            Allowed = rule.Allowed,

            Days = rule.Days
                .Select(day => new AccessDay
                {
                    Id = Guid.NewGuid(),
                    Day = day
                })
                .ToList(),

            Schedules = rule.Schedules
                .Select(schedule => new AccessSchedule
                {
                    Id = Guid.NewGuid(),
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime
                })
                .ToList()
        };

        await _repository.AddAsync(entity);

        _logger.LogInformation(
            "Access rule created. RuleId: {RuleId}",
            entity.Id);

        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, AccessRuleInputDto rule)
    {
        ValidateRuleInput(rule);

        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return false;

        var camera = await _repository.GetCameraByIdAsync(rule.CameraId);

        if (camera == null)
            throw new InvalidOperationException("Camera não encontrada.");

        if (!Guid.TryParse(rule.UserId, out var userId))
            throw new InvalidOperationException("UserId inválido.");

        existing.UserId = userId;
        existing.CameraId = camera.Id;
        existing.Camera = camera;
        existing.Allowed = rule.Allowed;

        existing.Days.Clear();

        foreach (var day in rule.Days.Distinct())
        {
            existing.Days.Add(new AccessDay
            {
                Id = Guid.NewGuid(),
                Day = day,
                AccessRuleId = existing.Id
            });
        }

        existing.Schedules.Clear();

        foreach (var schedule in rule.Schedules)
        {
            existing.Schedules.Add(new AccessSchedule
            {
                Id = Guid.NewGuid(),
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                AccessRuleId = existing.Id
            });
        }

        await _repository.UpdateAsync(existing);

        _logger.LogInformation(
            "Access rule updated. RuleId: {RuleId}",
            existing.Id);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return false;

        await _repository.DeleteAsync(id);

        _logger.LogInformation(
            "Access rule deleted. RuleId: {RuleId}",
            id);

        return true;
    }

    private static AccessRuleDto MapToDto(AccessRule rule)
    {
        return new AccessRuleDto
        {
            Id = rule.Id,
            UserId = rule.UserId.ToString(),
            CameraId = rule.CameraId,
            CameraName = rule.Camera?.Name ?? string.Empty,
            Allowed = rule.Allowed,

            Days = rule.Days
                .Select(d => (int)d.Day)
                .ToList(),

            Schedules = rule.Schedules
                .Select(s => new AccessScheduleDto
                {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList()
        };
    }

    private static void ValidateRuleInput(AccessRuleInputDto rule)
    {
        if (rule.Days == null || !rule.Days.Any())
            throw new InvalidOperationException(
                "A regra deve possuir ao menos um dia.");

        if (rule.Schedules == null || !rule.Schedules.Any())
            throw new InvalidOperationException(
                "A regra deve possuir ao menos um horário.");

        if (rule.Days.Any(d => d < 0 || d > 6))
            throw new InvalidOperationException(
                "Dias inválidos.");

        foreach (var schedule in rule.Schedules)
        {
            if (schedule.StartTime >= schedule.EndTime)
            {
                throw new InvalidOperationException(
                    "Horário inválido.");
            }
        }
    }
}