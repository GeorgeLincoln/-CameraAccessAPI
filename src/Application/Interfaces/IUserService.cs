using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(UserInputDto dto);
    Task<bool> UpdateAsync(Guid id, UserInputDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> LinkCameraAsync(Guid userId, Guid cameraId);
    Task<bool> UnlinkCameraAsync(Guid userId, Guid cameraId);
}
