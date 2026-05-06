namespace CameraAccessAPI.Domain.Entities;

public class AccessSchedule
{
    public int Id { get; set; }

    public Guid AccessRuleId { get; set; }
    public AccessRule AccessRule { get; set; } = default!;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}