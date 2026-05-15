namespace CameraAccessAPI.Application.DTOs;

/// <summary>
/// DTO para requisição de validação de token de stream
/// Utilizada pelo MediaMTX no auth hook HTTP
/// </summary>
public class StreamTokenValidationRequestDto
{
    /// <summary>
    /// Token JWT extraído da query string (?token=)
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Nome da câmera/stream (extraído do path)
    /// Exemplo: /test → "test"
    /// </summary>
    public required string StreamName { get; set; }

    /// <summary>
    /// IP do cliente (origem da requisição)
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Path completo da requisição (ex: /test)
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Método HTTP (GET, POST, etc)
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Timestamp da validação (UTC)
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Resposta padronizada para validação de token
/// MediaMTX espera: 200 OK para permitir, 401/403 para bloquear
/// </summary>
public class StreamTokenValidationResponseDto
{
    /// <summary>
    /// Indica se o acesso é permitido
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Motivo da decisão (para logging)
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário (extraído do token)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// ID da câmera validada
    /// </summary>
    public Guid? CameraId { get; set; }

    /// <summary>
    /// Timestamp de processamento
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
