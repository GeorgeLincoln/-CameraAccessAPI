using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("access-rules")]
public class AccessRulesController : ControllerBase
{
    private readonly IAccessRuleService _accessRuleService;
    private readonly ILogger<AccessRulesController> _logger;

    public AccessRulesController(
        IAccessRuleService accessRuleService,
        ILogger<AccessRulesController> logger)
    {
        _accessRuleService = accessRuleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rules = await _accessRuleService.GetAllAsync();
        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var rule = await _accessRuleService.GetByIdAsync(id);
        if (rule == null)
            return NotFound();

        return Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccessRuleInputDto rule)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _accessRuleService.CreateAsync(rule);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha ao criar regra de acesso");
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AccessRuleInputDto rule)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _accessRuleService.UpdateAsync(id, rule);
            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha ao atualizar regra de acesso");
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _accessRuleService.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
