using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ToTable("AccessLogs");

        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_AccessLogs_Timestamp");
        builder.HasIndex(x => new { x.UserId, x.CameraId, x.Timestamp })
            .HasDatabaseName("IX_AccessLogs_User_Camera_Timestamp");

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Source)
            .HasMaxLength(100);

        builder.HasOne(x => x.User)
            .WithMany(u => u.AccessLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Camera)
            .WithMany(c => c.AccessLogs)
            .HasForeignKey(x => x.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
