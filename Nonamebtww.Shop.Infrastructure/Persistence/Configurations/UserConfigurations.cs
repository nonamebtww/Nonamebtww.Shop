using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role> {
  public void Configure(EntityTypeBuilder<Role> builder) {
    builder.ToTable("roles");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);

    // Kind — альтернативный ключ (AK1), на него ссылается Пользователь.
    builder.HasAlternateKey(x => x.Kind);
    builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
    builder.Property(x => x.Description).HasMaxLength(512);
  }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
  public void Configure(EntityTypeBuilder<User> builder) {
    builder.ToTable("users");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Ignore(x => x.DomainEvents);
    builder.Ignore(x => x.FullName);

    builder.Property(x => x.Login).HasConversion(ValueObjectConverters.Login).HasMaxLength(32).IsRequired();
    builder.Property(x => x.Email).HasConversion(ValueObjectConverters.Email).HasMaxLength(254).IsRequired();
    builder.Property(x => x.Phone).HasConversion((ValueConverter)ValueObjectConverters.Phone).HasMaxLength(16);
    builder.HasIndex(x => x.Login).IsUnique();
    builder.HasIndex(x => x.Email).IsUnique();
    builder.HasIndex(x => x.Phone).IsUnique();

    builder.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
    builder.Property(x => x.FirstName).HasMaxLength(64).IsRequired();
    builder.Property(x => x.LastName).HasMaxLength(64);
    builder.Property(x => x.BlockReason).HasMaxLength(512);

    // Пользователь → Роль по альтернативному ключу Role.Kind (IDEF1X «назначена»).
    builder.HasOne<Role>()
      .WithMany()
      .HasForeignKey(x => x.Role)
      .HasPrincipalKey(r => r.Kind)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasMany(x => x.Devices).WithOne().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.Devices).HasField("_devices").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

    builder.HasMany(x => x.ConfirmationCodes).WithOne().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
    builder.Navigation(x => x.ConfirmationCodes)
      .HasField("_confirmationCodes")
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .AutoInclude();
  }
}

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device> {
  public void Configure(EntityTypeBuilder<Device> builder) {
    builder.ToTable("devices");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    builder.Property(x => x.IpAddress).HasMaxLength(64);
    builder.Property(x => x.RefreshToken).HasMaxLength(1024);
  }
}

internal sealed class ConfirmationCodeConfiguration : IEntityTypeConfiguration<ConfirmationCode> {
  public void Configure(EntityTypeBuilder<ConfirmationCode> builder) {
    builder.ToTable("confirmation_codes");
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).ValueGeneratedNever();
    builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
  }
}
