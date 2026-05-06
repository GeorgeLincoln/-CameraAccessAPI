namespace CameraAccessAPI.Application.Interfaces;

public interface IAccessService
{
    Task<bool> HasAccessAsync(string userId, string camera, DateTime now);
}