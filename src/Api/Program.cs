using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Application.Services;
using CameraAccessAPI.Domain.Interfaces;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using CameraAccessAPI.Infrastructure.Persistence.Repositories;
using CameraAccessAPI.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);
ConfigureServices(builder);

var app = builder.Build();

await EnsureDatabaseConnectionAsync(app);

ConfigureMiddleware(app);

Log.Information("API started successfully");

app.Run();


// ------------------------ CONFIGURATION ------------------------

void ConfigureLogging(WebApplicationBuilder builder)
{
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
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Camera Access API",
            Version = "v1",
            Description = "API for camera-based access control with schedule validation"
        });
    });

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    });

    builder.Services.AddScoped<IAccessRuleRepository, AccessRuleRepository>();
    builder.Services.AddScoped<IAccessRuleService, AccessRuleService>();
    builder.Services.AddScoped<IAccessService, AccessService>();
    builder.Services.AddSingleton<IJwtService, JwtService>();

    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
}


// ------------------------ DATABASE ------------------------

async Task EnsureDatabaseConnectionAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(3);

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation("Database connection established");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection attempt {Attempt} failed", attempt);
        }

        await Task.Delay(delay);
    }

    logger.LogCritical("Database connection failed after {Retries} attempts", maxRetries);
    throw new Exception("Database unavailable");
}


// ------------------------ MIDDLEWARE ------------------------

void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseIpRateLimiting();

    app.UseMiddleware<CameraAccessAPI.Api.Middlewares.ExceptionMiddleware>();

    app.UseHttpsRedirection();

    app.MapControllers();
}