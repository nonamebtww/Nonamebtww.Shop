using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nonamebtww.Shop.Application.Entities;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Configurations;

internal sealed class CacheConfiguration : IEntityTypeConfiguration<Cache> {
  public void Configure(EntityTypeBuilder<Cache> builder) {
    builder.ToTable("cache");
    builder.HasKey(x => x.Key);
    builder.Property(x => x.Key).HasMaxLength(512);
    builder.Property(x => x.Payload).IsRequired();
  }
}
