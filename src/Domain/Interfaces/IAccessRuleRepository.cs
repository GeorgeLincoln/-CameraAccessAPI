using CameraAccessAPI.Domain.Entities;

namespace CameraAccessAPI.Domain.Interfaces;

public interface IAccessRuleRepository
{
    Task<IEnumerable<AccessRule>> GetRulesAsync(string userId, string camera);
    Task<IEnumerable<AccessRule>> GetAllAsync();
    Task<AccessRule?> GetByIdAsync(Guid id);
    Task<Camera?> GetCameraByIdAsync(Guid cameraId);
    Task AddAsync(AccessRule rule);
    Task UpdateAsync(AccessRule rule);
    Task DeleteAsync(Guid id);
}