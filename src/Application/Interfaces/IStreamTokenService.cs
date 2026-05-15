using CameraAccessAPI.Application.DTOs;

namespace CameraAccessAPI.Application.Interfaces;

public interface IStreamTokenService
{
    Task<StreamTokenClaimsDto?> ValidateAndExtractClaimsAsync(
        string token,
        CancellationToken cancellationToken = default);
    Task<bool> IsTokenRevokedAsync(
        string tokenId,
        CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(
        string tokenId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);
}
