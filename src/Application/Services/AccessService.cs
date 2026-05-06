using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

public class AccessService : IAccessService
{
    private readonly IAccessRuleRepository _repository;
    private readonly ILogger<AccessService> _logger;

    public AccessService(
        IAccessRuleRepository repository,
        ILogger<AccessService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> HasAccessAsync(
        string userId,
        string camera,
        DateTime now)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            _logger.LogWarning("Invalid UserId format: {UserId}. Must be a GUID.", userId);
            return false;
        }

        var day = (int)now.DayOfWeek;
        var time = now.TimeOfDay;

        var rules = await _repository.GetRulesAsync(userIdGuid, camera);

        if (rules == null || !rules.Any())
        {
            _logger.LogWarning("No rules found for user {User}", userId);
            return false;
        }

        foreach (var rule in rules)
        {
            if (!rule.Allowed || !rule.Active)
                continue;

            // Optional: If DaysOfWeek is empty, it means all days.
            // But to keep it similar, we check if it contains the day.
            var validDay = rule.DaysOfWeek == null || !rule.DaysOfWeek.Any() || rule.DaysOfWeek.Contains(day);
            if (!validDay)
                continue;

            var validTime = time >= rule.StartTime && time <= rule.EndTime;

            if (validTime)
                return true;
        }

        return false;
    }
}