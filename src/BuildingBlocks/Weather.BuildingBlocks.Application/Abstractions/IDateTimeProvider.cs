namespace Weather.BuildingBlocks.Application.Abstractions;

/// <summary>Supplies the current instant; abstracted so handlers are deterministic in tests.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
