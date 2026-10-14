using Weather.BuildingBlocks.Application.Abstractions;

namespace Weather.BuildingBlocks.Infrastructure;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
