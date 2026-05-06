namespace CameraAccessAPI.Domain.Entities;

public class Camera
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public string? RtspUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // 🔁 Relacionamento (1:N)
    public ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
}