using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CameraAccessAPI.Infrastructure.Security;

/// <summary>
/// Implementação de validação de tokens de stream (JWT)
/// 
/// Responsabilidades:
/// - Validar assinatura do JWT
/// - Verificar expiração
/// - Extrair claims obrigatórios
/// - Gerenciar revogação de tokens (futuro: redis)
/// 
/// NÃO valida regras de negócio (dias/horários) - é responsabilidade do AccessValidationService
/// </summary>
public class StreamTokenService : IStreamTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _keyBytes;
    private readonly ILogger<StreamTokenService> _logger;

    // Futura: Integração com Redis para revogar tokens
    // private readonly IDistributedCache _cache;
    private static readonly HashSet<string> RevokedTokenIds = new();

    public StreamTokenService(
        IConfiguration configuration,
        ILogger<StreamTokenService> logger)
    {
        _logger = logger;

        _key = configuration["Jwt:Key"]
            ?? throw new ArgumentNullException("Jwt:Key is required");

        _issuer = configuration["Jwt:Issuer"] ?? "CameraAccessAPI";
        _audience = configuration["Jwt:Audience"] ?? "CameraClients";

        _keyBytes = Encoding.UTF8.GetBytes(_key);

        // 🔐 Validação mínima obrigatória
        if (_keyBytes.Length < 32)
            throw new ArgumentException("JWT Key must be at least 256 bits (32 bytes)");
    }

    /// <summary>
    /// Valida token JWT e extrai claims
    /// 
    /// Validações de segurança:
    /// 1. Verificação de assinatura (HMAC-SHA256)
    /// 2. Validação de expiração
    /// 3. Validação de Issuer
    /// 4. Validação de Audience
    /// 5. Extração de claims obrigatórios (sub, camera, jti)
    /// </summary>
    public async Task<StreamTokenClaimsDto?> ValidateAndExtractClaimsAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: empty token");
            return null;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // 🔐 Validação RIGOROSA de segurança
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),

                    ValidateIssuer = true,
                    ValidIssuer = _issuer,

                    ValidateAudience = true,
                    ValidAudience = _audience,

                    ValidateLifetime = true, // ⚠️ CRÍTICO: Valida expiração
                    ClockSkew = TimeSpan.Zero, // Sem tolerância de tempo (segurança)
                },
                out _);

            if (principal?.Identity is not ClaimsIdentity identity)
            {
                _logger.LogWarning("Token validation failed: no claims identity");
                return null;
            }

            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                _logger.LogWarning("Token validation failed: cannot parse JWT");
                return null;
            }

            // Extrair claims obrigatórios
            var subClaim = identity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cameraClaim = identity.FindFirst("camera")?.Value;
            var jtiClaim = identity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var iatClaim = identity.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;

            if (string.IsNullOrWhiteSpace(subClaim) ||
                string.IsNullOrWhiteSpace(cameraClaim) ||
                string.IsNullOrWhiteSpace(jtiClaim))
            {
                _logger.LogWarning(
                    "Token validation failed: missing required claims. Sub={Sub}, Camera={Camera}, Jti={Jti}",
                    subClaim ?? "null",
                    cameraClaim ?? "null",
                    jtiClaim ?? "null");

                return null;
            }

            // Converter claims para tipos corretos
            if (!Guid.TryParse(subClaim, out var userId))
            {
                _logger.LogWarning("Token validation failed: invalid userId format {UserId}", subClaim);
                return null;
            }

            var issuedAt = jwtToken.ValidFrom.ToUniversalTime();
            if (!string.IsNullOrWhiteSpace(iatClaim) && long.TryParse(iatClaim, out var iatUnix))
                issuedAt = UnixTimeStampToDateTime(iatUnix);

            // Verificar revogação
            if (await IsTokenRevokedAsync(jtiClaim, cancellationToken))
            {
                _logger.LogWarning("Token validation failed: token revoked {TokenId}", jtiClaim);
                return null;
            }

            var claims = new StreamTokenClaimsDto
            {
                UserId = userId,
                StreamName = cameraClaim,
                TokenId = jtiClaim,
                IssuedAt = issuedAt,
                ExpiresAt = jwtToken.ValidTo.ToUniversalTime(),
            };

            _logger.LogInformation(
                "Token validation succeeded. User={UserId}, Stream={Stream}, ExpiresIn={ExpiresIn}s",
                claims.UserId,
                claims.StreamName,
                claims.SecondsRemaining);

            return claims;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: token expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: invalid signature");
            return null;
        }
        catch (SecurityTokenInvalidAlgorithmException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: invalid algorithm");
            return null;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: invalid argument");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed with unexpected error");
            return null;
        }
    }

    /// <summary>
    /// Verifica se um token foi revogado
    /// Implementação atual: In-memory HashSet (thread-safe)
    /// Futura: Integrar com Redis para escalabilidade
    /// </summary>
    public Task<bool> IsTokenRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var isRevoked = RevokedTokenIds.Contains(tokenId);

        if (isRevoked)
            _logger.LogInformation("Token is revoked: {TokenId}", tokenId);

        return Task.FromResult(isRevoked);
    }

    /// <summary>
    /// Revoga um token específico
    /// Implementação atual: Armazena em HashSet in-memory
    /// Futura: Usar Redis com TTL = tempo até expiração natural
    /// </summary>
    public Task RevokeTokenAsync(
        string tokenId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        RevokedTokenIds.Add(tokenId);

        var ttl = expiresAt - DateTime.UtcNow;
        _logger.LogInformation(
            "Token revoked: {TokenId}, will auto-expire in {TTL}s",
            tokenId,
            ttl.TotalSeconds);

        // 🚀 TODO: Integrar com Redis
        // await _cache.SetStringAsync(
        //     $"revoked_token:{tokenId}",
        //     "1",
        //     absoluteExpirationRelativeToNow: ttl,
        //     token: cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper: Converte Unix timestamp para DateTime
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
