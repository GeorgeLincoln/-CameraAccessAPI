namespace CameraAccessAPI.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(string userId, string camera);
}