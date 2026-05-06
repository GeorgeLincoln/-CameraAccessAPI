using CameraAccessAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CameraAccessAPI.Infrastructure.Persistence.Configurations;

public class UserCameraConfiguration : IEntityTypeConfiguration<UserCamera>
{
    public void Configure(EntityTypeBuilder<UserCamera> builder)
    {
        builder.ToTable("UserCameras");

        builder.HasKey(x => new { x.UserId, x.CameraId });

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserCameras)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Camera)
            .WithMany(c => c.UserCameras)
            .HasForeignKey(x => x.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
