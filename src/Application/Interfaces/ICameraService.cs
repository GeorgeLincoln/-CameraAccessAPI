using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface ICameraService
{
    Task<IEnumerable<CameraDto>> GetAllAsync();
    Task<CameraDto?> GetByIdAsync(Guid id);
    Task<CameraDto> CreateAsync(CameraInputDto dto);
    Task<bool> UpdateAsync(Guid id, CameraInputDto dto);
    Task<bool> DeleteAsync(Guid id);
}
