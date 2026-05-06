namespace CameraAccessAPI.Domain.Entities;

public class AccessDay
{
    public int Id { get; set; }

    public Guid AccessRuleId { get; set; }
    public AccessRule AccessRule { get; set; } = default!;

    public int DayOfWeek { get; set; } // 0–6
}