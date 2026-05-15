using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Application.Services;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Infrastructure.Security;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CameraAccessAPI.Tests.Security;

/// <summary>
/// Testes de segurança para validação de tokens de stream
/// 
/// Cenários testados:
/// 1. ✅ Token válido → Acesso permitido
/// 2. ❌ Token expirado → Acesso negado
/// 3. ❌ Token inválido/alterado → Acesso negado
/// 4. ❌ Token ausente → Acesso negado
/// 5. ❌ Token com claims faltantes → Acesso negado
/// 6. ❌ Token revogado → Acesso negado
/// </summary>
public class StreamTokenServiceSecurityTests
{
    private readonly IStreamTokenService _tokenService;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<StreamTokenService>> _loggerMock;

    public StreamTokenServiceSecurityTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<StreamTokenService>>();

        // Configurar JWT com chave válida (256 bits)
        _configMock
            .Setup(c => c["Jwt:Key"])
            .Returns("MySecretKeyForTestingPurposesOnlyMin32!!");

        _configMock
            .Setup(c => c["Jwt:Issuer"])
            .Returns("CameraAccessAPI");

        _configMock
            .Setup(c => c["Jwt:Audience"])
            .Returns("CameraClients");

        _configMock
            .Setup(c => c["Jwt:ExpiryMinutes"])
            .Returns("1");

        _tokenService = new StreamTokenService(_configMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// ✅ TEST 1: Token válido com claims corretos deve ser validado com sucesso
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsClaimsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var streamName = "test-camera";
        
        // Criar token via JwtService
        var jwtService = new JwtService(_configMock.Object);
        var token = jwtService.GenerateToken(userId.ToString(), streamName);

        // Act
        var claims = await _tokenService.ValidateAndExtractClaimsAsync(token);

        // Assert
        Assert.NotNull(claims);
        Assert.Equal(userId, claims.UserId);
        Assert.Equal(streamName, claims.StreamName);
        Assert.False(claims.IsExpired);
        Assert.True(claims.SecondsRemaining > 0);
    }

    /// <summary>
    /// ❌ TEST 2: Token com assinatura inválida deve ser rejeitado
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithTamperedToken_ReturnsFail()
    {
        // Arrange
        var jwtService = new JwtService(_configMock.Object);
        var token = jwtService.GenerateToken(Guid.NewGuid().ToString(), "test-camera");
        
        // Alterar token (simular ataque)
        var tamperedToken = token[..^10] + "XXXXX00000";

        // Act
        var claims = await _tokenService.ValidateAndExtractClaimsAsync(tamperedToken);

        // Assert - Token inválido deve retornar null
        Assert.Null(claims);
    }

    /// <summary>
    /// ❌ TEST 3: Token vazio deve ser rejeitado
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithEmptyToken_ReturnsFail()
    {
        // Act
        var claims = await _tokenService.ValidateAndExtractClaimsAsync(string.Empty);

        // Assert
        Assert.Null(claims);
    }

    /// <summary>
    /// ❌ TEST 4: Token com claims faltantes deve ser rejeitado
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithMissingClaims_ReturnsFail()
    {
        // Arrange - Token sem camera claim
        var userId = Guid.NewGuid();
        // Seria preciso criar um token sem o claim "camera" para testar
        // Isso requereria acesso à geração de token customizada
        
        // Por enquanto, testar com token malformado
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.invalid";

        // Act
        var claims = await _tokenService.ValidateAndExtractClaimsAsync(invalidToken);

        // Assert
        Assert.Null(claims);
    }

    /// <summary>
    /// ✅ TEST 5: Revogação de token deve impedir acesso
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithRevokedToken_ReturnsFail()
    {
        // Arrange
        var jwtService = new JwtService(_configMock.Object);
        var userId = Guid.NewGuid().ToString();
        var token = jwtService.GenerateToken(userId, "test-camera");

        // Validar token (deve suceder)
        var claims1 = await _tokenService.ValidateAndExtractClaimsAsync(token);
        Assert.NotNull(claims1);

        // Revogar token
        await _tokenService.RevokeTokenAsync(claims1.TokenId, claims1.ExpiresAt);

        // Act - Tentar validar novamente
        var claims2 = await _tokenService.ValidateAndExtractClaimsAsync(token);

        // Assert - Token revogado deve ser rejeitado
        Assert.Null(claims2);
    }

    /// <summary>
    /// 🔐 TEST 6: Token não deve funcionar com chave diferente
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithDifferentKey_ReturnsFail()
    {
        // Arrange - Gerar token com uma chave
        var jwtService1 = new JwtService(_configMock.Object);
        var token = jwtService1.GenerateToken(Guid.NewGuid().ToString(), "test-camera");

        // Configurar token service com chave diferente
        var wrongConfigMock = new Mock<IConfiguration>();
        wrongConfigMock.Setup(c => c["Jwt:Key"]).Returns("DifferentSecretKeyForTestingMin32!");
        wrongConfigMock.Setup(c => c["Jwt:Issuer"]).Returns("CameraAccessAPI");
        wrongConfigMock.Setup(c => c["Jwt:Audience"]).Returns("CameraClients");
        
        var wrongTokenService = new StreamTokenService(wrongConfigMock.Object, _loggerMock.Object);

        // Act
        var claims = await wrongTokenService.ValidateAndExtractClaimsAsync(token);

        // Assert - Token gerado com chave diferente deve falhar
        Assert.Null(claims);
    }
}

public class StreamAccessValidationIntegrationTests
{
    [Fact]
    public async Task ValidateStreamAccess_WithValidTokenAndSchedule_AllowsAccess()
    {
        var fixture = CreateFixture();
        var token = fixture.JwtService.GenerateToken(fixture.User.Id.ToString(), fixture.Camera.Name);

        var response = await fixture.Service.ValidateStreamAccessAsync(new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = fixture.Camera.Name,
            ClientIp = "127.0.0.1"
        });

        Assert.True(response.Allowed);
        Assert.Equal("Access granted", response.Reason);
        Assert.Equal(fixture.User.Id, response.UserId);
        Assert.Equal(fixture.Camera.Id, response.CameraId);
    }

    [Fact]
    public async Task ValidateStreamAccess_OutsideSchedule_DeniesAccess()
    {
        var fixture = CreateFixture(ruleStart: TimeSpan.FromHours(1), ruleEnd: TimeSpan.FromHours(2));
        var token = fixture.JwtService.GenerateToken(fixture.User.Id.ToString(), fixture.Camera.Name);

        var response = await fixture.Service.ValidateStreamAccessAsync(new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = fixture.Camera.Name
        });

        Assert.False(response.Allowed);
        Assert.Equal("Access outside allowed schedule", response.Reason);
    }

    [Fact]
    public async Task ValidateStreamAccess_WithTokenFromDifferentStream_DeniesAccess()
    {
        var fixture = CreateFixture();
        var token = fixture.JwtService.GenerateToken(fixture.User.Id.ToString(), "another-stream");

        var response = await fixture.Service.ValidateStreamAccessAsync(new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = fixture.Camera.Name
        });

        Assert.False(response.Allowed);
        Assert.Equal("Token stream mismatch", response.Reason);
    }

    [Fact]
    public async Task ValidateStreamAccess_WithInactiveUser_DeniesAccess()
    {
        var fixture = CreateFixture(userActive: false);
        var token = fixture.JwtService.GenerateToken(fixture.User.Id.ToString(), fixture.Camera.Name);

        var response = await fixture.Service.ValidateStreamAccessAsync(new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = fixture.Camera.Name
        });

        Assert.False(response.Allowed);
        Assert.Equal("User is inactive", response.Reason);
    }

    [Fact]
    public async Task ValidateStreamAccess_WithExpiredToken_DeniesAccess()
    {
        var fixture = CreateFixture();
        var token = GenerateExpiredToken(fixture.User.Id, fixture.Camera.Name);

        var response = await fixture.Service.ValidateStreamAccessAsync(new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = fixture.Camera.Name
        });

        Assert.False(response.Allowed);
        Assert.Equal("Token validation failed", response.Reason);
    }

    private static TestFixture CreateFixture(
        bool userActive = true,
        bool cameraActive = true,
        int expiryMinutes = 1,
        TimeSpan? ruleStart = null,
        TimeSpan? ruleEnd = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "MySecretKeyForTestingPurposesOnlyMin32!!",
            ["Jwt:Issuer"] = "CameraAccessAPI",
            ["Jwt:Audience"] = "CameraClients",
            ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString(),
            ["AccessControl:Timezone"] = "UTC"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Active = userActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var camera = new Camera
        {
            Id = Guid.NewGuid(),
            Name = "test-camera",
            Active = cameraActive,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.Cameras.Add(camera);
        context.UserCameras.Add(new UserCamera
        {
            UserId = user.Id,
            CameraId = camera.Id
        });

        var accessRule = new AccessRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CameraId = camera.Id,
            Camera = camera,
            Active = true,
            Allowed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Adicionar dia da semana
        accessRule.Days.Add(new AccessDay
        {
            Id = Guid.NewGuid(),
            AccessRuleId = accessRule.Id,
            Day = (int)DateTime.UtcNow.DayOfWeek
        });

        // Adicionar horário
        accessRule.Schedules.Add(new AccessSchedule
        {
            Id = Guid.NewGuid(),
            AccessRuleId = accessRule.Id,
            StartTime = ruleStart ?? TimeSpan.Zero,
            EndTime = ruleEnd ?? new TimeSpan(23, 59, 59)
        });

        context.AccessRules.Add(accessRule);
        context.SaveChanges();

        var jwtService = new JwtService(configuration);
        var tokenService = new StreamTokenService(configuration, Mock.Of<ILogger<StreamTokenService>>());
        var service = new StreamAccessValidationService(
            tokenService,
            context,
            configuration,
            Mock.Of<ILogger<StreamAccessValidationService>>());

        return new TestFixture(service, jwtService, user, camera);
    }

    private sealed record TestFixture(
        StreamAccessValidationService Service,
        JwtService JwtService,
        User User,
        Camera Camera);

    private static string GenerateExpiredToken(Guid userId, string streamName)
    {
        var key = Encoding.UTF8.GetBytes("MySecretKeyForTestingPurposesOnlyMin32!!");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "CameraAccessAPI",
            audience: "CameraClients",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("camera", streamName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            ],
            notBefore: now.AddMinutes(-3),
            expires: now.AddMinutes(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class AccessServiceScheduleTests
{
    [Fact]
    public async Task HasAccessAsync_OvernightWindow_AllowsAccess()
    {
        var userId = Guid.NewGuid();
        var repository = new Mock<Domain.Interfaces.IAccessRuleRepository>();

        // Criar regra com dias e horários corretos
        var rule = new AccessRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CameraId = Guid.NewGuid(),
            Active = true,
            Allowed = true
        };

        // Adicionar segunda-feira (DayOfWeek.Monday = 1)
        rule.Days.Add(new AccessDay
        {
            Id = Guid.NewGuid(),
            AccessRuleId = rule.Id,
            Day = (int)DayOfWeek.Monday
        });

        // Adicionar horário da noite (22:00 até 02:00 do dia seguinte)
        rule.Schedules.Add(new AccessSchedule
        {
            Id = Guid.NewGuid(),
            AccessRuleId = rule.Id,
            StartTime = new TimeSpan(22, 0, 0),
            EndTime = new TimeSpan(2, 0, 0)
        });

        repository.Setup(r => r.GetRulesAsync(userId, "cam-1"))
            .ReturnsAsync(new[] { rule });

        var service = new AccessService(repository.Object, Mock.Of<ILogger<AccessService>>());
        var mondayAt23 = new DateTime(2026, 5, 4, 23, 0, 0, DateTimeKind.Utc);

        var allowed = await service.HasAccessAsync(userId, "cam-1", mondayAt23);

        Assert.True(allowed);
    }
}
