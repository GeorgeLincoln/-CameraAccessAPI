namespace CameraAccessAPI.Domain.Entities;

public class AccessLog
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid CameraId { get; set; }
    public Camera Camera { get; set; } = default!;

    public DateTime Timestamp { get; set; }
    public bool Allowed { get; set; }
    public string Reason { get; set; } = default!;
    public string Source { get; set; } = default!;
}
