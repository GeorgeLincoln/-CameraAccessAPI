using System;

namespace CameraAccessAPI.Application.DTOs;

public class AccessLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = default!;
    public Guid CameraId { get; set; }
    public string CameraName { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public bool Allowed { get; set; }
    public string Reason { get; set; } = default!;
    public string Source { get; set; } = default!;
}
