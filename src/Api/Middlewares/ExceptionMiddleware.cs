using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net;

namespace CameraAccessAPI.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString("N");

            if (ex is NpgsqlException npgEx)
            {
                _logger.LogError(npgEx, "🔴 Erro PostgreSQL CorrelationId: {CorrelationId} | SqlState: {SqlState} | Message: {Message}",
                    correlationId, npgEx.SqlState, npgEx.Message);
            }
            else
            {
                _logger.LogError(ex, "🔴 Erro não tratado CorrelationId: {CorrelationId}", correlationId);
            }

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var root = GetInnermostException(exception);
        var errorCode = GetErrorCode(root);
        var message = GetUserFacingMessage(root);
        var suggestions = GetDebugSuggestions(root);

        var result = new
        {
            error = message,
            code = errorCode,
            correlationId,
            details = root.Message,
            suggestions = suggestions
        };

        return context.Response.WriteAsJsonAsync(result);
    }

    private static Exception GetInnermostException(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            DbUpdateException => "DATABASE_UPDATE_FAILURE",
            NpgsqlException npg when npg.SqlState == "28P01" => "DATABASE_AUTHENTICATION_FAILURE",
            NpgsqlException npg when npg.SqlState == "3D000" => "DATABASE_NOT_FOUND",
            NpgsqlException => "DATABASE_CONNECTION_FAILURE",
            InvalidDataException => "CONFIGURATION_PARSE_ERROR",
            _ => "UNHANDLED_EXCEPTION"
        };
    }

    private static string GetUserFacingMessage(Exception exception)
    {
        return exception switch
        {
            DbUpdateException => "Erro de banco de dados. A solicitação falhou devido a um problema de persistência.",
            NpgsqlException npg when npg.SqlState == "28P01" => "Falha de autenticação com o banco de dados. Verifique a senha.",
            NpgsqlException npg when npg.SqlState == "3D000" => "Banco de dados não encontrado.",
            NpgsqlException => "Erro de conexão com o banco de dados. Verifique se o PostgreSQL está rodando.",
            InvalidDataException => "Arquivo de configuração inválido. Verifique a sintaxe do appsettings.",
            _ => "Erro interno do servidor. Use o correlationId para rastrear o problema."
        };
    }

    private static string[] GetDebugSuggestions(Exception exception)
    {
        return exception switch
        {
            DbUpdateException => new[]
            {
                "✅ Verifique se o modelo da entidade está correto",
                "✅ Verifique as configurações EF Core em Infrastructure/Persistence/Configurations",
                "✅ Execute: dotnet ef database update"
            },
            NpgsqlException npg when npg.SqlState == "28P01" => new[]
            {
                "✅ Verifique a senha em appsettings.Development.json",
                "✅ Confirme que a senha no docker-compose.yml está igual",
                "✅ Reinicie o container: docker-compose restart postgres",
                "✅ Verifique: docker exec camera_access_db psql -U postgres -c 'SELECT 1'"
            },
            NpgsqlException npg when npg.SqlState == "3D000" => new[]
            {
                "✅ Crie o banco de dados com docker exec camera_access_db psql -U postgres",
                "✅ Verifique o nome do banco em appsettings.json"
            },
            NpgsqlException => new[]
            {
                "✅ Verifique se o Docker está rodando: docker ps",
                "✅ Inicie o container: docker-compose up -d",
                "✅ Verifique os logs: docker-compose logs postgres"
            },
            InvalidDataException => new[]
            {
                "✅ Verifique a sintaxe do appsettings.json ou appsettings.Development.json",
                "✅ Validar JSON em https://jsonlint.com"
            },
            _ => new[]
            {
                "✅ Verifique os logs: tail -f logs/log-*.txt",
                "✅ Use o correlationId para rastrear a aplicação"
            }
        };
    }
}
