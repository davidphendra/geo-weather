namespace Weather.Modules.Geography.Domain;

/// <summary>Thrown when a Geography domain invariant is violated.</summary>
public sealed class GeographyDomainException : Exception
{
    public GeographyDomainException(string message) : base(message)
    {
    }
}
