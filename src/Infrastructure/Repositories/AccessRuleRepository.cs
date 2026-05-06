using CameraAccessAPI.Domain.Entities;
using CameraAccessAPI.Domain.Interfaces;
using CameraAccessAPI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace CameraAccessAPI.Infrastructure.Persistence.Repositories;

public class AccessRuleRepository : IAccessRuleRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccessRuleRepository> _logger;

    public AccessRuleRepository(
        AppDbContext context,
        ILogger<AccessRuleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 🔥 MÉTODO PRINCIPAL (usado pelo AccessService)
    public async Task<IEnumerable<AccessRule>> GetRulesAsync(Guid userId, string camera)
    {
        try
        {
            _logger.LogDebug(
                "🔍 Buscando regras - User: {UserId}, Camera: {Camera}",
                userId, camera);

            var rules = await _context.AccessRules
                .AsNoTracking()
                .Include(r => r.Camera)
                .Where(r =>
                    r.UserId == userId &&
                    (r.CameraId == null || r.Camera!.Name == camera))
                .ToListAsync();

            _logger.LogDebug(
                "✅ {Count} regras encontradas para User: {UserId}, Camera: {Camera}",
                rules.Count, userId, camera);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Erro ao buscar regras - User: {UserId}, Camera: {Camera}",
                userId, camera);

            throw;
        }
    }

    // 🔽 Métodos auxiliares (opcional manter)

    public async Task<IEnumerable<AccessRule>> GetAllAsync()
    {
        try
        {
            return await _context.AccessRules
                .AsNoTracking()
                .Include(r => r.Camera)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao consultar todas as regras");
            throw;
        }
    }

    public async Task<AccessRule?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.AccessRules
                .Include(r => r.Camera)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao buscar regra por id: {RuleId}", id);
            throw;
        }
    }

    public async Task<Camera?> GetCameraByIdAsync(Guid cameraId)
    {
        try
        {
            return await _context.Cameras.FindAsync(cameraId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao buscar câmera por id: {CameraId}", cameraId);
            throw;
        }
    }

    public async Task AddAsync(AccessRule rule)
    {
        try
        {
            await _context.AccessRules.AddAsync(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Regra criada: {RuleId} - User: {UserId}",
                rule.Id, rule.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Erro ao criar regra para usuário: {UserId}",
                rule.UserId);
            throw;
        }
    }

    public async Task UpdateAsync(AccessRule rule)
    {
        try
        {
            _context.AccessRules.Update(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Regra atualizada: {RuleId}",
                rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Erro ao atualizar regra: {RuleId}",
                rule.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var rule = await _context.AccessRules.FindAsync(id);

            if (rule == null)
            {
                _logger.LogWarning("⚠️ Regra não encontrada: {RuleId}", id);
                return;
            }

            _context.AccessRules.Remove(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Regra removida: {RuleId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Erro ao remover regra: {RuleId}",
                id);
            throw;
        }
    }
}