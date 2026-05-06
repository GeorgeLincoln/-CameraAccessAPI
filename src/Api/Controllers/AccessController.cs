using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("api/access")]
public class AccessController : ControllerBase
{
    private readonly IAccessValidationService _validationService;

    public AccessController(IAccessValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateAccess([FromBody] AccessValidationRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var response = await _validationService.ValidateAccessAsync(request);
        return Ok(response);
    }
}
