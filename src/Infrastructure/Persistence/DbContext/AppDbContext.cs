using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CameraAccessAPI.Infrastructure.Persistence.Contexts;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Camera> Cameras { get; set; } = null!;
    public DbSet<AccessRule> AccessRules { get; set; } = null!;
    public DbSet<AccessDay> AccessDays { get; set; } = null!;
    public DbSet<AccessSchedule> AccessSchedules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}