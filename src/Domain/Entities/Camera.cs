namespace CameraAccessAPI.Domain.Entities;

public class Camera
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public string? RtspUrl { get; set; }

    public bool Active { get; set; } = true;

    public int Status { get; set; } = 0;

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // 🔁 Relacionamentos (1:N)
    public ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public ICollection<UserCamera> UserCameras { get; set; } = new List<UserCamera>();
    public ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
}