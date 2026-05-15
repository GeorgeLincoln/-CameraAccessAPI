namespace CameraAccessAPI.Domain.Entities;

public class AccessSchedule
{
    public Guid Id { get; set; }

    public Guid AccessRuleId { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public AccessRule AccessRule { get; set; } = null!;
}