using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CameraAccessAPI.Infrastructure.Security;

public class JwtService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly byte[] _keyBytes;

    public JwtService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"] 
            ?? throw new ArgumentNullException("Jwt:Key is required");

        _issuer = configuration["Jwt:Issuer"] ?? "CameraAccessAPI";
        _audience = configuration["Jwt:Audience"] ?? "CameraClients";

        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var minutes)
            ? minutes
            : 5;

        _keyBytes = Encoding.UTF8.GetBytes(_key);

        // 🔐 Segurança mínima obrigatória (256 bits)
        if (_keyBytes.Length < 32)
            throw new ArgumentException("JWT Key must be at least 256 bits (32 bytes)");
    }

    public string GenerateToken(string userId, string camera)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId is required");

        if (string.IsNullOrWhiteSpace(camera))
            throw new ArgumentException("camera is required");

        userId = userId.Trim();
        camera = camera.Trim();

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            // Identidade
            new Claim(JwtRegisteredClaimNames.Sub, userId),

            // Contexto do domínio (ESSENCIAL)
            new Claim("camera", camera),

            // Segurança
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_keyBytes),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}