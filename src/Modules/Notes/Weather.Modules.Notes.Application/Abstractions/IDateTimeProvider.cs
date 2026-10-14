namespace Weather.Modules.Notes.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
