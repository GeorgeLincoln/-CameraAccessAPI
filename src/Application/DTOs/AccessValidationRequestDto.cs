using System;

namespace CameraAccessAPI.Application.DTOs;

public class AccessValidationRequestDto
{
    public Guid UserId { get; set; }
    public Guid CameraId { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class AccessValidationResponseDto
{
    public bool Allowed { get; set; }
    public string Reason { get; set; } = default!;
}
