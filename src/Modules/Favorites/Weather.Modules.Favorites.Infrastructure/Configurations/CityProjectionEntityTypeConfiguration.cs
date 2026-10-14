using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Favorites.Domain.Cities;

namespace Weather.Modules.Favorites.Infrastructure.Configurations;

internal sealed class CityProjectionEntityTypeConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CityProjections");
        builder.HasKey(city => city.CityId);
        builder.Property(city => city.CityId)
            .HasConversion(id => id.Value, value => CityId.From(value));
        builder.Property(city => city.CountryCode)
            .HasConversion(code => code.Value, value => CountryCode.From(value))
            .HasMaxLength(2);
        builder.Property(city => city.Name)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(city => city.Coordinate)
            .HasConversion(
                coordinate => GeoCoordinateConverter.ToProvider(coordinate),
                value => GeoCoordinateConverter.FromProvider(value))
            .HasMaxLength(64)
            .IsRequired();
    }
}
