using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface IStreamAccessValidationService
{
    Task<StreamTokenValidationResponseDto> ValidateStreamAccessAsync(
        StreamTokenValidationRequestDto request,
        CancellationToken cancellationToken = default);
    Task<Guid?> ResolveStreamNameToCameraIdAsync(
        string streamName,
        CancellationToken cancellationToken = default);
}
