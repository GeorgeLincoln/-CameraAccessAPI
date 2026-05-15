namespace CameraAccessAPI.Application.DTOs;

/// <summary>
/// Claims extraídos do token JWT (privado para aplicação)
/// Usado internamente para validação de acesso
/// </summary>
public class StreamTokenClaimsDto
{
    /// <summary>
    /// ID do usuário (claim: sub)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Nome da câmera/stream (claim: camera)
    /// </summary>
    public string StreamName { get; set; } = string.Empty;

    /// <summary>
    /// ID único da emissão do token (claim: jti)
    /// Previne replay attacks
    /// </summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de emissão (claim: iat)
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Timestamp de expiração (claim: exp)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Se o token está expirado
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Segundos restantes até expiração
    /// </summary>
    public int SecondsRemaining => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
}
