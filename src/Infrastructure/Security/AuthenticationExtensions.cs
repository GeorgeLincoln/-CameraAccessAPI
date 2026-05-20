using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CameraAccessAPI.Infrastructure.Security;

/// <summary>
/// Extensão para registrar autenticação JWT no container de DI
/// Centraliza toda a configuração de segurança relacionada a JWT
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adiciona autenticação JWT ao container de DI com validações obrigatórias
    /// </summary>
    /// <param name="services">IServiceCollection para registro de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <returns>IServiceCollection para encadeamento fluente</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registrar e validar configurações
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        var jwtSettings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(jwtSettings);
        
        try
        {
            jwtSettings.ValidateConfiguration();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Configuração JWT inválida. Verifique appsettings.json", ex);
        }

        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);

        // Registrar JwtService
        services.AddScoped<JwtService>();

        // Adicionar autenticação JWT
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // 🔒 Validações obrigatórias
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // ASSINATURA
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                    // ESTRUTURA DO TOKEN
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    // TEMPO DE VIDA
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // ⚠️ CRÍTICO: sem tolerância de tempo

                    // OBRIGATÓRIAS
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                };

                // 🔐 Não salvar token em cookies/session (defesa contra XSS)
                options.SaveToken = false;

                // ✅ HTTPS obrigatório em produção
                options.RequireHttpsMetadata = !configuration.GetValue<bool>("Jwt:AllowInsecureHttp", false);

                // Manipuladores de eventos para logging seguro
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Logging desabilitado para evitar exposição de dados sensíveis em produção
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        // Token foi validado com sucesso
                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        // Desafio de autenticação acionado
                        return Task.CompletedTask;
                    }
                };
            });

        // Adicionar autorização
        services.AddAuthorization(options =>
        {
            // Política padrão: requer autenticação
            options.AddPolicy("AuthenticatedOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            // Política para acesso a câmeras específicas
            options.AddPolicy("CameraAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("camera");
            });
        });

        return services;
    }
}
