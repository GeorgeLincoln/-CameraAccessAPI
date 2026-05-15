namespace CameraAccessAPI.Domain.Entities;

public class AccessDay
{
    public Guid Id { get; set; }

    public Guid AccessRuleId { get; set; }

    public int Day { get; set; }

    public AccessRule AccessRule { get; set; } = null!;
}