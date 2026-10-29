using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ReviewAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category> {
  public void Configure(EntityTypeBuilder<Category> builder) {
    builder.ToTable("categories");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);
    builder.Ignore(x => x.IsRoot);

    builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    builder.Property(x => x.Description).HasMaxLength(2048);
    builder.Property(x => x.Photo).HasMaxLength(1024);

    // Дерево категорий (IDEF1X «входит в», необязательный родитель).
    builder.HasOne<Category>()
      .WithMany()
      .HasForeignKey(x => x.ParentId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product> {
  public void Configure(EntityTypeBuilder<Product> builder) {
    builder.ToTable("products");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);
    builder.Ignore(x => x.IsAvailable);

    builder.Property(x => x.Sku).HasConversion(ValueObjectConverters.Sku).HasMaxLength(64).IsRequired();
    builder.HasIndex(x => x.Sku).IsUnique();
    builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
    builder.Property(x => x.Description).HasMaxLength(4096);
    builder.Property(x => x.Characteristics).HasMaxLength(4096);
    builder.Property(x => x.Photo).HasMaxLength(1024);
    builder.ComplexProperty(x => x.Price, money => money.MapMoney("price"));
    builder.HasIndex(x => x.Name);

    builder.HasOne<Category>()
      .WithMany()
      .HasForeignKey(x => x.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review> {
  public void Configure(EntityTypeBuilder<Review> builder) {
    builder.ToTable("reviews");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);

    builder.Property(x => x.Rating).HasConversion(ValueObjectConverters.Rating).IsRequired();
    builder.Property(x => x.Text).HasMaxLength(4000);
    // Не уникальный: у пользователя может быть несколько отзывов на один товар.
    builder.HasIndex(x => new { x.UserId, x.ProductId });

    builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
  }
}
