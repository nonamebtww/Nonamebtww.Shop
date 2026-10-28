using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nonamebtww.Shop.Domain.Aggregates.CartAggregate;
using Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;
using Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart> {
  public void Configure(EntityTypeBuilder<Cart> builder) {
    builder.ToTable("carts");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);
    builder.Ignore(x => x.IsEmpty);
    builder.Ignore(x => x.TotalQuantity);

    builder.HasIndex(x => x.UserId).IsUnique();
    builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(x => x.Items).WithOne().HasForeignKey("CartId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
  }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem> {
  public void Configure(EntityTypeBuilder<CartItem> builder) {
    // IDEF1X «Позиция корзины».
    builder.ToTable("cart_items");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.HasIndex("CartId", nameof(CartItem.ProductId)).IsUnique();
    builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
  }
}

internal sealed class FavoritesConfiguration : IEntityTypeConfiguration<Favorites> {
  public void Configure(EntityTypeBuilder<Favorites> builder) {
    builder.ToTable("favorites");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);

    builder.HasIndex(x => x.UserId).IsUnique();
    builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(x => x.Items).WithOne().HasForeignKey("FavoritesId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
  }
}

internal sealed class FavoriteItemConfiguration : IEntityTypeConfiguration<FavoriteItem> {
  public void Configure(EntityTypeBuilder<FavoriteItem> builder) {
    // IDEF1X «Избранное».
    builder.ToTable("favorite_items");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.HasIndex("FavoritesId", nameof(FavoriteItem.ProductId)).IsUnique();
    builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
  }
}
