using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CameraAccessAPI.Infrastructure.Security;

/// <summary>
/// Serviço seguro de geração e validação de tokens JWT
/// </summary>
public class JwtService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _keyBytes;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> options, ILogger<JwtService> logger)
    {
        _logger = logger;
        _settings = options.Value;

        // Validar configurações obrigatoriamente no construtor
        try
        {
            _settings.ValidateConfiguration();
            _keyBytes = Encoding.UTF8.GetBytes(_settings.Key);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogCritical("Falha ao validar configurações JWT: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gera um token JWT seguro com as claims obrigatórias
    /// </summary>
    /// <param name="userId">Identificador único do usuário</param>
    /// <param name="camera">Identificador único da câmera</param>
    /// <param name="additionalClaims">Claims adicionais opcionais</param>
    /// <returns>Token JWT assinado</returns>
    /// <exception cref="ArgumentException">Se userId ou camera forem inválidos</exception>
    public string GenerateToken(string userId, string camera, Dictionary<string, string>? additionalClaims = null)
    {
        // Validar entradas
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId é obrigatório", nameof(userId));

        if (string.IsNullOrWhiteSpace(camera))
            throw new ArgumentException("camera é obrigatório", nameof(camera));

        userId = userId.Trim();
        camera = camera.Trim();

        var now = DateTime.UtcNow;
        var jti = Guid.NewGuid().ToString();

        try
        {
            var claims = new List<Claim>
            {
                // OBRIGATÓRIAS - Claims padrão JWT
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64),

                // ESSENCIAL - Contexto de domínio
                new Claim("camera", camera),
            };

            // Claims adicionais (se fornecidos)
            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    if (!string.IsNullOrWhiteSpace(claim.Key) && !string.IsNullOrWhiteSpace(claim.Value))
                    {
                        claims.Add(new Claim(claim.Key, claim.Value));
                    }
                }
            }

            // Credenciais de assinatura com HS256
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(_keyBytes),
                SecurityAlgorithms.HmacSha256);

            // Construir token
            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Logging seguro (sem expor o token completo)
            _logger.LogInformation(
                "Token JWT gerado com sucesso. JTI: {JTI}, UserId: {UserId}, Camera: {Camera}, Expires: {ExpiryTime}",
                jti, userId, camera, token.ValidTo);

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar token JWT para UserId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Obtém as configurações ativas do JWT (para testes/debug)
    /// </summary>
    public JwtSettings GetSettings() => _settings;
}
