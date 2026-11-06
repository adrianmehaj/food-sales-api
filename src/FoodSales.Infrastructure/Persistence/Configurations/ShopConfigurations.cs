using FoodSales.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodSales.Infrastructure.Persistence.Configurations;

public sealed class ShopConfigurations : IEntityTypeConfiguration<Product>, IEntityTypeConfiguration<Customer>,
    IEntityTypeConfiguration<CustomerPrice>, IEntityTypeConfiguration<Order>, IEntityTypeConfiguration<OrderLine>,
    IEntityTypeConfiguration<Payment>, IEntityTypeConfiguration<OrderEvent>, IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1500).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.OfferPrice).HasPrecision(18, 2);
        b.Property(x => x.Stock).HasPrecision(18, 3);
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => new { x.IsActive, x.Name });
        b.ToTable(t => { t.HasCheckConstraint("CK_Product_Price", "[Price] > 0"); t.HasCheckConstraint("CK_Product_Stock", "[Stock] >= 0"); });
    }
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.UserId).HasMaxLength(450);
        b.HasOne<IdentityUser>().WithOne().HasForeignKey<Customer>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(25).IsRequired();
        b.Property(x => x.City).HasMaxLength(80).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => x.Name); b.HasIndex(x => x.Phone);
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
    }
    public void Configure(EntityTypeBuilder<CustomerPrice> b)
    {
        b.HasKey(x => new { x.CustomerId, x.ProductId });
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_CustomerPrice_Positive", "[Price] > 0"));
    }
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.RequestId).IsUnique();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(450);
        b.Property(x => x.CreditApprovedBy).HasMaxLength(450);
        b.Property(x => x.CustomerName).HasMaxLength(120).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(25).IsRequired();
        b.Property(x => x.City).HasMaxLength(80).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Version).IsRowVersion();
        b.Ignore(x => x.Total); b.Ignore(x => x.Paid); b.Ignore(x => x.Debt); b.Ignore(x => x.PaymentStatus);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.History).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.CompletedAt);
    }
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ProductName).HasMaxLength(120).IsRequired();
        b.Property(x => x.RequestedQuantity).HasPrecision(18, 3);
        b.Property(x => x.FinalQuantity).HasPrecision(18, 3);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Total).HasPrecision(18, 2);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_OrderLine_Positive", "[FinalQuantity] > 0 AND [UnitPrice] > 0"));
    }
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.HasIndex(x => x.RequestId).IsUnique(); b.HasIndex(x => x.ReceivedAt);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.ReceivedBy).HasMaxLength(450).IsRequired();
        b.Property(x => x.Note).HasMaxLength(300).IsRequired();
        b.ToTable(t => t.HasCheckConstraint("CK_Payment_Positive", "[Amount] > 0"));
    }
    public void Configure(EntityTypeBuilder<OrderEvent> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.ActorId).HasMaxLength(450);
        b.HasIndex(x => new { x.OrderId, x.At });
    }
    public void Configure(EntityTypeBuilder<Banner> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Text).HasMaxLength(500).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        b.PrimitiveCollection(x => x.ImageUrls).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.IsActive, x.EndsAt });
    }
}
