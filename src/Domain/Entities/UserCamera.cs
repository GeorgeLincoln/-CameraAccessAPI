namespace CameraAccessAPI.Domain.Entities;

public class UserCamera
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid CameraId { get; set; }
    public Camera Camera { get; set; } = default!;
}
