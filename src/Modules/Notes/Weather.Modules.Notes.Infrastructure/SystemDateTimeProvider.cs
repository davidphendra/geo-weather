using Weather.Modules.Notes.Application.Abstractions;

namespace Weather.Modules.Notes.Infrastructure;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
