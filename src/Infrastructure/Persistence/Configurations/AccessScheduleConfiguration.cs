using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class AccessScheduleConfiguration : IEntityTypeConfiguration<AccessSchedule>
{
    public void Configure(EntityTypeBuilder<AccessSchedule> builder)
    {
        builder.ToTable("AccessSchedules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();
    }
}