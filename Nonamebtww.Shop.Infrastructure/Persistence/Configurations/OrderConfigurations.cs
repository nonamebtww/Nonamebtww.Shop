using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nonamebtww.Shop.Domain.Aggregates.OrderAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order> {
  public void Configure(EntityTypeBuilder<Order> builder) {
    builder.ToTable("orders");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);
    builder.Ignore(x => x.CheckoutStep);
    builder.Ignore(x => x.Lines);
    builder.Ignore(x => x.SucceededPayment);
    builder.Ignore(x => x.TotalQuantity);

    builder.HasIndex(x => x.UserId);
    builder.HasIndex(x => x.Status);
    builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

    builder.ComplexProperty(x => x.Total, money => money.MapMoney("total"));

    builder.ComplexProperty(x => x.DeliveryAddress, address => {
      address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(128);
      address.Property(a => a.Street).HasColumnName("address_street").HasMaxLength(256);
      address.Property(a => a.House).HasColumnName("address_house").HasMaxLength(32);
      address.Property(a => a.Apartment).HasColumnName("address_apartment").HasMaxLength(32);
      address.Property(a => a.PostalCode).HasColumnName("address_postal_code").HasMaxLength(16);
      address.Property(a => a.Comment).HasColumnName("address_comment").HasMaxLength(512);
    });

    builder.HasMany(x => x.Items).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

    builder.HasMany(x => x.Payments).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Payments).HasField("_payments").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

    // Доставка 0..1, её id совпадает с id заказа (IDEF1X «доставляется», Z).
    builder.HasOne(x => x.Delivery)
      .WithOne()
      .HasForeignKey<Delivery>(d => d.Id)
      .OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Delivery).AutoInclude();
  }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem> {
  public void Configure(EntityTypeBuilder<OrderItem> builder) {
    builder.ToTable("order_items");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.Subtotal);
    builder.ComplexProperty(x => x.UnitPrice, money => money.MapMoney("unit_price"));
    builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
  }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment> {
  public void Configure(EntityTypeBuilder<Payment> builder) {
    builder.ToTable("payments");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.ComplexProperty(x => x.Amount, money => money.MapMoney("amount"));
    builder.Property(x => x.TransactionId).HasMaxLength(128);
    builder.Property(x => x.FailureReason).HasMaxLength(512);
  }
}

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery> {
  public void Configure(EntityTypeBuilder<Delivery> builder) {
    builder.ToTable("deliveries");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Property(x => x.FailureReason).HasMaxLength(512);
    builder.HasIndex(x => x.CourierId);

    // Курьер — пользователь с ролью «Курьер» (IDEF1X «выполняет»).
    builder.HasOne<User>().WithMany().HasForeignKey(x => x.CourierId).OnDelete(DeleteBehavior.Restrict);
  }
}
