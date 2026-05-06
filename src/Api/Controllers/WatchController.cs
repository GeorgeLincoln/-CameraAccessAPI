using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("watch")]
public class WatchController : ControllerBase
{
    private readonly IAccessService _accessService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<WatchController> _logger;

    public WatchController(
        IAccessService accessService,
        IJwtService jwtService,
        ILogger<WatchController> logger)
    {
        _accessService = accessService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Gera URL de acesso ao stream se o usuário estiver autorizado
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccess(
        [FromQuery] string userId,
        [FromQuery] string camera)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(camera))
            return BadRequest("userId e camera são obrigatórios");

        var now = DateTime.UtcNow;

        _logger.LogInformation(
            "Access request - User: {UserId}, Camera: {Camera}",
            userId, camera);

        var hasAccess = await _accessService.HasAccessAsync(userId, camera, now);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Access denied - User: {UserId}, Camera: {Camera}",
                userId, camera);

            return Unauthorized("Acesso negado");
        }

        // Token vinculado à câmera (correto)
        var token = _jwtService.GenerateToken(userId, camera);

        var streamUrl = $"http://localhost:8888/{camera}?token={token}";

        _logger.LogInformation(
            "Access granted - User: {UserId}, Camera: {Camera}",
            userId, camera);

        return Ok(new AccessResponseDto
        {
            Stream = streamUrl,
            ExpiresInSeconds = 300
        });
    }
}