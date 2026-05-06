namespace CameraAccessAPI.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Document { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserCamera> UserCameras { get; set; } = new List<UserCamera>();
    public ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
}
