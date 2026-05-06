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
            .IsRequired();

        builder.Property(x => x.Allowed)
            .IsRequired();
            
        builder.Property(x => x.Active)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.DaysOfWeek)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Camera)
            .WithMany(c => c.AccessRules)
            .HasForeignKey(x => x.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.User)
            .WithMany(u => u.AccessRules)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}