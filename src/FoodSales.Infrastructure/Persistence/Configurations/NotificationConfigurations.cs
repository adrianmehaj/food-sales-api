using FoodSales.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodSales.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfigurations : IEntityTypeConfiguration<PlatformNotification>, IEntityTypeConfiguration<NotificationRead>
{
    public void Configure(EntityTypeBuilder<PlatformNotification> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RecipientUserId).HasMaxLength(450);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
        b.HasIndex(x => new { x.ForAdministrators, x.CreatedAt });
    }
    public void Configure(EntityTypeBuilder<NotificationRead> b)
    {
        b.HasKey(x => new { x.NotificationId, x.UserId });
        b.Property(x => x.UserId).HasMaxLength(450);
        b.HasOne<PlatformNotification>().WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.UserId);
    }
}
