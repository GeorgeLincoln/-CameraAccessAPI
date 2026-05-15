namespace CameraAccessAPI.Application.Interfaces;

public interface IAccessService
{
    Task<bool> HasAccessAsync(Guid userId, string camera, DateTime now);
}