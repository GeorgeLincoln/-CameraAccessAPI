using CameraAccessAPI.Application.DTOs;
using CameraAccessAPI.Application.Interfaces;
using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Domain.Enums;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CameraAccessAPI.Application.Services;

public class CameraService : ICameraService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CameraService> _logger;

    public CameraService(AppDbContext context, ILogger<CameraService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CameraDto>> GetAllAsync()
    {
        var cameras = await _context.Cameras.AsNoTracking().ToListAsync();
        return cameras.Select(MapToDto);
    }

    public async Task<CameraDto?> GetByIdAsync(Guid id)
    {
        var camera = await _context.Cameras.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return camera == null ? null : MapToDto(camera);
    }

    public async Task<CameraDto> CreateAsync(CameraInputDto dto)
    {
        var camera = new Camera
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Location = dto.Location,
            RtspUrl = dto.RtspUrl,
            Active = dto.Active,
            Status = 0,
            LastSeenAt = null
        };

        _context.Cameras.Add(camera);
        await _context.SaveChangesAsync();

        return MapToDto(camera);
    }

    public async Task<bool> UpdateAsync(Guid id, CameraInputDto dto)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null) return false;

        camera.Name = dto.Name;
        camera.Location = dto.Location;
        camera.RtspUrl = dto.RtspUrl;
        camera.Active = dto.Active;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var camera = await _context.Cameras.Include(c => c.AccessRules).FirstOrDefaultAsync(c => c.Id == id);
        if (camera == null) return false;

        if (camera.AccessRules.Any(r => r.Active))
        {
            throw new InvalidOperationException("Não é possível deletar câmera vinculada a regras ativas.");
        }

        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();
        return true;
    }

    private static CameraDto MapToDto(Camera camera)
    {
        return new CameraDto
        {
            Id = camera.Id,
            Name = camera.Name,
            Location = camera.Location,
            RtspUrl = camera.RtspUrl,
            Active = camera.Active,
            LastSeenAt = camera.LastSeenAt,
        };
    }
}
