using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class AccessRuleConfiguration : IEntityTypeConfiguration<AccessRule>
{
    public void Configure(EntityTypeBuilder<AccessRule> builder)
    {
        builder.ToTable("AccessRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Allowed)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Camera)
            .WithMany(c => c.AccessRules)
            .HasForeignKey(x => x.CameraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Days)
            .WithOne(d => d.AccessRule)
            .HasForeignKey(d => d.AccessRuleId);

        builder.HasMany(x => x.Schedules)
            .WithOne(s => s.AccessRule)
            .HasForeignKey(s => s.AccessRuleId);
    }
}