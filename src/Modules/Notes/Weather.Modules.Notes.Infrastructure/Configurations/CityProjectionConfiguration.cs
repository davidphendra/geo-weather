using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.Modules.Notes.Domain.Cities;
using Weather.Modules.Notes.Domain.Countries;

namespace Weather.Modules.Notes.Infrastructure.Configurations;

internal sealed class CityProjectionConfiguration : IEntityTypeConfiguration<City>
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
    }
}
