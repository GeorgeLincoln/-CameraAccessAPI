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
        var day = (int)now.DayOfWeek;
        var time = now.TimeOfDay;

        var rules = await _repository.GetRulesAsync(userId, camera);

        if (rules == null || !rules.Any())
        {
            _logger.LogWarning("No rules found for user {User}", userId);
            return false;
        }

        foreach (var rule in rules)
        {
            if (!rule.Allowed)
                continue;

            var validDay = rule.Days.Any(d => d.DayOfWeek == day);
            if (!validDay)
                continue;

            var validTime = rule.Schedules.Any(s =>
                time >= s.StartTime && time <= s.EndTime);

            if (validTime)
                return true;
        }

        return false;
    }
}