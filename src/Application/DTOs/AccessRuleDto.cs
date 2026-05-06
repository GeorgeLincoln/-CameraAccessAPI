namespace CameraAccessAPI.Application.DTOs;

public sealed class AccessRuleDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public Guid CameraId { get; set; }
    public string CameraName { get; set; } = default!;
    public bool Allowed { get; set; }
    public IReadOnlyCollection<int> Days { get; set; } = Array.Empty<int>();
    public IReadOnlyCollection<AccessScheduleDto> Schedules { get; set; } = Array.Empty<AccessScheduleDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AccessScheduleDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
