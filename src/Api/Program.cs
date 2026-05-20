using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using CameraAccessAPI.Infrastructure.Persistence.Repositories;
using CameraAccessAPI.Application.Services;
using CameraAccessAPI.Domain.Interfaces;
using CameraAccessAPI.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

#region LOGGING (Serilog)

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File(
            "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);
});

#endregion

#region SERVICES

builder.Services.AddControllers();

// 🔐 AUTENTICAÇÃO JWT (segura e completa)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger (OpenAPI) com suporte JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Camera Access API",
        Version = "v1",
        Description = "API para controle de acesso por câmera com validação de horário",
    });
});

// Database (PostgreSQL + Retry Policy)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// Repositories
builder.Services.AddScoped<IAccessRuleRepository, AccessRuleRepository>();

// Services
builder.Services.AddScoped<AccessService>();

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

#endregion

var app = builder.Build();

#region DATABASE CHECK (Fail Fast)

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync())
        {
            throw new Exception("Database unreachable");
        }

        logger.LogInformation("✅ PostgreSQL conectado com sucesso");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ Falha crítica ao conectar com o banco de dados");
        throw; // interrompe a aplicação
    }
}

#endregion

#region MIDDLEWARE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Camera Access API v1");
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
    });
}

// HTTPS redirection (CRÍTICO para produção)
app.UseHttpsRedirection();

// Segurança e Rate Limiting
app.UseIpRateLimiting();

// Tratamento global de erro
app.UseMiddleware<CameraAccessAPI.Api.Middlewares.ExceptionMiddleware>();

// 🔐 PIPELINE DE AUTENTICAÇÃO (ORDEM IMPORTANTE!)
// 1. Autenticar o usuário (valida JWT)
app.UseAuthentication();

// 2. Autorizar o usuário (verifica [Authorize])
app.UseAuthorization();

// Mapear controllers
app.MapControllers();

#endregion

Log.Information("🚀 API iniciada com sucesso");
app.Run();
