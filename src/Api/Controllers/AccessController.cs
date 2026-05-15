using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("api/access")]
public class AccessController : ControllerBase
{
    private readonly IAccessValidationService _validationService;
    private readonly IStreamAccessValidationService _streamValidationService;
    private readonly ILogger<AccessController> _logger;
    private readonly IConfiguration _configuration;

    public AccessController(
        IAccessValidationService validationService,
        IStreamAccessValidationService streamValidationService,
        IConfiguration configuration,
        ILogger<AccessController> logger)
    {
        _validationService = validationService;
        _streamValidationService = streamValidationService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validação de acesso tradicional (com autenticação Bearer)
    /// Utilizada pelo frontend/clientes autenticados
    /// </summary>
    [Authorize]
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateAccess([FromBody] AccessValidationRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var response = await _validationService.ValidateAccessAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Validação de acesso para streams (auth hook do MediaMTX)
    /// 
    /// Endpoint chamado pelo MediaMTX para cada tentativa de acesso ao stream
    /// Não requer autenticação Bearer (MediaMTX não envia isso)
    /// 
    /// Fluxo:
    /// 1. Cliente: GET /streamname?token=JWT
    /// 2. MediaMTX intercepta e chama: POST /api/access/stream/validate?token=JWT&stream=streamname&ip=X.X.X.X
    /// 3. Backend: Valida token, regras, e retorna 200 OK ou 401
    /// 4. MediaMTX: Permite ou bloqueia acesso
    /// 
    /// 🔐 CRÍTICO: Nenhuma lógica de segurança no frontend!
    /// </summary>
    [AllowAnonymous]
    [HttpPost("stream/validate")]
    public async Task<IActionResult> ValidateStreamAccess(
        [FromQuery] string? token,
        [FromQuery] string? stream,
        [FromQuery] string? ip,
        [FromQuery] string? path)
    {
        // Optional hardening: if configured, only calls with shared secret are accepted.
        var hookSecret = _configuration["StreamValidation:HookSecret"];
        if (!string.IsNullOrWhiteSpace(hookSecret))
        {
            var providedSecret = Request.Headers.TryGetValue("X-Stream-Auth", out var headerSecret)
                ? headerSecret.ToString()
                : Request.Query["hookSecret"].ToString();

            if (string.IsNullOrWhiteSpace(providedSecret) ||
                !ConstantTimeEquals(providedSecret, hookSecret))
            {
                _logger.LogWarning("Stream validation rejected: invalid shared hook secret");
                return Unauthorized(new { status = "error", reason = "Invalid hook authentication" });
            }
        }

        // ⚠️ Validações básicas
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(stream))
        {
            _logger.LogWarning("Stream validation failed: missing token or stream parameter");
            return Unauthorized(new { status = "error", reason = "Missing token or stream" });
        }

        var request = new StreamTokenValidationRequestDto
        {
            Token = token,
            StreamName = stream,
            ClientIp = ip ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            Path = path ?? HttpContext.Request.Path.ToString(),
            Method = HttpContext.Request.Method,
            RequestedAt = DateTime.UtcNow,
        };

        _logger.LogInformation(
            "Stream access validation. Stream={Stream}, ClientIp={ClientIp}",
            stream,
            request.ClientIp);

        var response = await _streamValidationService.ValidateStreamAccessAsync(request);

        // MediaMTX espera:
        // - 200 OK para permitir acesso
        // - 401/403 para bloquear acesso
        if (response.Allowed)
        {
            _logger.LogInformation("✅ Stream access allowed. UserId={UserId}", response.UserId);
            return Ok(new { status = "ok" }); // MediaMTX interpreta como "permitir"
        }

        _logger.LogWarning("❌ Stream access denied. Reason={Reason}", response.Reason);
        return Unauthorized(new { status = "error", reason = response.Reason }); // 401
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var left = Encoding.UTF8.GetBytes(a);
        var right = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
