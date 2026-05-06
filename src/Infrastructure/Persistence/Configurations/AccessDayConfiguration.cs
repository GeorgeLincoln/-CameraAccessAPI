using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class AccessDayConfiguration : IEntityTypeConfiguration<AccessDay>
{
    public void Configure(EntityTypeBuilder<AccessDay> builder)
    {
        builder.ToTable("AccessDays");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.HasIndex(x => new { x.AccessRuleId, x.DayOfWeek })
            .IsUnique(); // evita duplicidade de dia por regra
    }
}