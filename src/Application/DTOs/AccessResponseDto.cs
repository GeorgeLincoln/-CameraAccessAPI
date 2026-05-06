namespace CameraAccessAPI.Application.DTOs;

public sealed class AccessResponseDto
{
    public string Stream { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
}