using Weather.BuildingBlocks.Domain;

namespace Weather.Modules.Geography.Domain;

/// <summary>Thrown when a Geography domain invariant is violated.</summary>
public sealed class GeographyDomainException : DomainException
{
    public GeographyDomainException(string message) : base(message)
    {
    }
}
