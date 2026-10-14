using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.Modules.Geography.Domain.Cities;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Infrastructure.Configurations;

internal sealed class CityEntityTypeConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Cities");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Id)
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
        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(city => city.CountryCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
