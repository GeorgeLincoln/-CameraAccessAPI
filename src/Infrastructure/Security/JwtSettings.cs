namespace CameraAccessAPI.Infrastructure.Security;

/// <summary>
/// Configurações seguras para JWT com validações obrigatórias
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Chave secreta para assinatura (mínimo 256 bits / 32 bytes)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Identificador do emissor do token
    /// </summary>
    public string Issuer { get; set; } = "CameraAccessAPI";

    /// <summary>
    /// Identificador do consumidor do token
    /// </summary>
    public string Audience { get; set; } = "CameraClients";

    /// <summary>
    /// Tempo de expiração em minutos (recomendado: 15-60 min)
    /// </summary>
    public int ExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Verificar se as configurações são válidas
    /// </summary>
    /// <exception cref="InvalidOperationException">Lançado quando configurações são inválidas</exception>
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("JWT Key é obrigatória e não pode estar vazia");

        var keyBytes = System.Text.Encoding.UTF8.GetBytes(Key);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                $"JWT Key deve ter no mínimo 256 bits (32 bytes). Atual: {keyBytes.Length} bytes");

        if (ExpiryMinutes < 1 || ExpiryMinutes > 1440)
            throw new InvalidOperationException(
                "JWT ExpiryMinutes deve estar entre 1 e 1440 minutos (1 dia)");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer é obrigatório");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience é obrigatório");
    }
}
