using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("Cameras");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.Location)
            .HasMaxLength(200);

        builder.Property(x => x.RtspUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Active)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();
            
        builder.HasMany(x => x.AccessRules)
            .WithOne(ar => ar.Camera!)
            .HasForeignKey(ar => ar.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(x => x.AccessLogs)
            .WithOne(al => al.Camera)
            .HasForeignKey(al => al.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}