using System;

namespace CameraAccessAPI.Application.DTOs;

public class CameraDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? RtspUrl { get; set; }
    public bool Active { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public int Status { get; set; }
}

public class CameraInputDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? RtspUrl { get; set; }
    public bool Active { get; set; } = true;
}
