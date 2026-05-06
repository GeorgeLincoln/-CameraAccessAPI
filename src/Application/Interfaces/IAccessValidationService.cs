using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface IAccessValidationService
{
    Task<AccessValidationResponseDto> ValidateAccessAsync(AccessValidationRequestDto request);
}
