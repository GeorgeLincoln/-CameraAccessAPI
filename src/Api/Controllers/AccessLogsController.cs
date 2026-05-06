using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CameraAccessAPI.Api.Controllers;

[ApiController]
[Route("api/access-logs")]
public class AccessLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccessLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? cameraId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.AccessLogs
            .AsNoTracking()
            .Include(l => l.User)
            .Include(l => l.Camera)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (cameraId.HasValue)
            query = query.Where(l => l.CameraId == cameraId.Value);

        if (startDate.HasValue)
            query = query.Where(l => l.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.Timestamp <= endDate.Value);

        var logs = await query.OrderByDescending(l => l.Timestamp).ToListAsync();

        var dtos = logs.Select(l => new AccessLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User?.Name ?? "Desconhecido",
            CameraId = l.CameraId,
            CameraName = l.Camera?.Name ?? "Desconhecida",
            Timestamp = l.Timestamp,
            Allowed = l.Allowed,
            Reason = l.Reason,
            Source = l.Source
        }).ToList();

        return Ok(dtos);
    }
}
