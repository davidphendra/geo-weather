using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.BuildingBlocks.Domain.SharedKernel;
using Weather.Modules.Geography.Domain.Countries;

namespace Weather.Modules.Geography.Infrastructure.Configurations;

internal sealed class CountryEntityTypeConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Countries");
        builder.HasKey(country => country.Code);
        builder.Property(country => country.Code)
            .HasConversion(code => code.Value, value => CountryCode.From(value))
            .HasMaxLength(2);
        builder.Property(country => country.Name)
            .HasMaxLength(100)
            .IsRequired();
    }
}
