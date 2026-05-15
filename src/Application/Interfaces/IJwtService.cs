namespace CameraAccessAPI.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, string camera);
}