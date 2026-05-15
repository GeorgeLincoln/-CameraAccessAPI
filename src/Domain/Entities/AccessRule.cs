namespace CameraAccessAPI.Domain.Entities;

public class AccessRule
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CameraId { get; set; }
    public Camera Camera { get; set; } = default!;

    public bool Allowed { get; set; } = true;
    public bool Active { get; set; } = true;

    public ICollection<AccessDay> Days { get; set; } = new List<AccessDay>();
    public ICollection<AccessSchedule> Schedules { get; set; } = new List<AccessSchedule>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}