using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.Modules.Favorites.Domain.Cities;
using Weather.Modules.Favorites.Domain.FavoriteCities;

namespace Weather.Modules.Favorites.Infrastructure.Configurations;

internal sealed class FavoriteCityEntityTypeConfiguration : IEntityTypeConfiguration<FavoriteCity>
{
    public void Configure(EntityTypeBuilder<FavoriteCity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FavoriteCities");
        builder.HasKey(favorite => favorite.Id);
        builder.Property(favorite => favorite.Id)
            .HasConversion(id => id.Value, value => FavoriteCityId.From(value));
        builder.Property(favorite => favorite.CityId)
            .HasConversion(id => id.Value, value => CityId.From(value));
        builder.Property(favorite => favorite.CreatedAtUtc).IsRequired();
        builder.HasIndex(favorite => favorite.CityId).IsUnique();
        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(favorite => favorite.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
