using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface IAccessRuleService
{
    Task<IEnumerable<AccessRuleDto>> GetAllAsync();
    Task<AccessRuleDto?> GetByIdAsync(Guid id);
    Task<AccessRuleDto> CreateAsync(AccessRuleInputDto rule);
    Task<bool> UpdateAsync(Guid id, AccessRuleInputDto rule);
    Task<bool> DeleteAsync(Guid id);
}
