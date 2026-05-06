namespace CameraAccessAPI.Domain.Entities;

public class AccessRule
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid? CameraId { get; set; }
    public Camera? Camera { get; set; }

    public bool Allowed { get; set; } = true;
    public bool Active { get; set; } = true;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    // DaysOfWeek as an array of integers (0=Sunday ... 6=Saturday)
    // Stored as a JSON array in Postgres, or flags. Let's use int[] and EF Core JSON mapping.
    public int[] DaysOfWeek { get; set; } = Array.Empty<int>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}