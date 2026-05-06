using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("api/cameras")]
public class CamerasController : ControllerBase
{
    private readonly ICameraService _cameraService;

    public CamerasController(ICameraService cameraService)
    {
        _cameraService = cameraService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cameras = await _cameraService.GetAllAsync();
        return Ok(cameras);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var camera = await _cameraService.GetByIdAsync(id);
        if (camera == null) return NotFound();
        return Ok(camera);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CameraInputDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _cameraService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CameraInputDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _cameraService.UpdateAsync(id, dto);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _cameraService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
