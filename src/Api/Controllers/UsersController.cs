using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserInputDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _userService.UpdateAsync(id, dto);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/cameras")]
    public async Task<IActionResult> LinkCamera(Guid id, [FromBody] Guid cameraId)
    {
        var linked = await _userService.LinkCameraAsync(id, cameraId);
        if (!linked) return BadRequest("Falha ao vincular câmera. Verifique se o usuário e a câmera existem.");
        return Ok();
    }

    [HttpDelete("{id:guid}/cameras/{cameraId:guid}")]
    public async Task<IActionResult> UnlinkCamera(Guid id, Guid cameraId)
    {
        var unlinked = await _userService.UnlinkCameraAsync(id, cameraId);
        if (!unlinked) return NotFound("Vínculo não encontrado.");
        return NoContent();
    }
}
