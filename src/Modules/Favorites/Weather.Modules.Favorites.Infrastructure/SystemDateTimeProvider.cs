using Weather.Modules.Favorites.Application.Abstractions;

namespace Weather.Modules.Favorites.Infrastructure;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
