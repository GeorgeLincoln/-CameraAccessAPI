using CameraAccessAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

/// <summary>
/// Controlador de autenticação JWT
/// Fornece endpoints para login e geração de tokens
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// DTO para requisição de login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Identificador único do usuário
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Identificador único da câmera a acessar
        /// </summary>
        public string Camera { get; set; } = string.Empty;

        /// <summary>
        /// Senha do usuário (validar conforme sua política de segurança)
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de login
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Token JWT para incluir no header Authorization: Bearer token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de token (sempre "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Tempo até expiração do token em segundos
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Hora UTC de expiração do token
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Endpoint de login seguro com geração de JWT
    /// 
    /// Fluxo:
    /// 1. Validar credenciais do usuário (implementar sua lógica)
    /// 2. Validar acesso à câmera (implementar sua lógica)
    /// 3. Gerar token JWT com contexto seguro
    /// 4. Retornar token com metadados
    /// 
    /// Segurança:
    /// - HTTPS obrigatório em produção
    /// - Validar credenciais contra banco de dados
    /// - Rate limiting recomendado
    /// - Logging de tentativas falhadas
    /// </summary>
    /// <param name="request">Credenciais do usuário</param>
    /// <returns>Token JWT válido</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Validar input
        if (request == null || string.IsNullOrWhiteSpace(request.UserId) || 
            string.IsNullOrWhiteSpace(request.Camera))
        {
            _logger.LogWarning("Tentativa de login com dados inválidos");
            return BadRequest(new { error = "UserId e Camera são obrigatórios" });
        }

        // 🔐 TODO: Implementar sua lógica de autenticação aqui
        // Exemplo simplificado (NUNCA usar em produção):
        
        // 1. Validar credenciais contra banco de dados
        // var user = await _userRepository.GetByIdAsync(request.UserId);
        // if (user == null || !_passwordHasher.VerifyHashedPassword(user, request.Password))
        // {
        //     _logger.LogWarning("Falha de autenticação para UserId: {UserId}", request.UserId);
        //     return Unauthorized(new { error = "Credenciais inválidas" });
        // }

        // 2. Validar permissão para acessar a câmera
        // var hasAccess = await _accessService.ValidateAccessAsync(request.UserId, request.Camera);
        // if (!hasAccess)
        // {
        //     _logger.LogWarning("Acesso negado à câmera. UserId: {UserId}, Camera: {Camera}", 
        //         request.UserId, request.Camera);
        //     return Unauthorized(new { error = "Sem permissão para acessar esta câmera" });
        // }

        // Para demonstração, aceitar qualquer combinação
        _logger.LogInformation("Login bem-sucedido. UserId: {UserId}, Camera: {Camera}", 
            request.UserId, request.Camera);

        try
        {
            // Gerar token JWT
            var token = _jwtService.GenerateToken(request.UserId, request.Camera);
            var jwtSettings = _jwtService.GetSettings();
            var expiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes);

            return Ok(new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = (int)(jwtSettings.ExpiryMinutes * 60),
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar token JWT");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Erro ao gerar token de autenticação" });
        }
    }

    /// <summary>
    /// Endpoint para validar um token JWT existente
    /// Útil para verificar se um token ainda é válido antes de usar
    /// </summary>
    /// <returns>Informações do token validado</returns>
    [HttpGet("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        var token = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { error = "Token não fornecido" });
        }

        // 🔐 TODO: Implementar validação real do token
        // var principal = _tokenValidator.ValidateToken(token);
        // if (principal == null)
        // {
        //     return Unauthorized(new { error = "Token inválido ou expirado" });
        // }

        var userId = User.FindFirst("sub")?.Value ?? "desconhecido";
        var camera = User.FindFirst("camera")?.Value ?? "desconhecida";

        return Ok(new
        {
            valid = true,
            userId,
            camera,
            message = "Token válido"
        });
    }

    /// <summary>
    /// Endpoint de teste para verificar se o servidor está acessível
    /// Não requer autenticação
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
