using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Weather.Modules.Notes.Domain.Countries;
using Weather.Modules.Notes.Domain.Notes;

namespace Weather.Modules.Notes.Infrastructure.Configurations;

internal sealed class WeatherNoteConfiguration : IEntityTypeConfiguration<WeatherNote>
{
    public void Configure(EntityTypeBuilder<WeatherNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("WeatherNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Id)
            .HasConversion(id => id.Value, value => WeatherNoteId.From(value));
        builder.Property(note => note.CityName)
            .HasMaxLength(WeatherNote.MaxCityNameLength)
            .IsRequired();
        builder.Property(note => note.CountryCode)
            .HasConversion(code => code.Value, value => CountryCode.From(value))
            .HasMaxLength(2);
        builder.Property(note => note.Note)
            .HasMaxLength(WeatherNote.MaxNoteLength)
            .IsRequired();
        builder.Property(note => note.TemperatureCelsius);
        builder.Property(note => note.CreatedAtUtc).IsRequired();
    }
}
