namespace Weather.BuildingBlocks.Domain;

/// <summary>Thrown when a domain invariant is violated. Mapped to ProblemDetails at the API boundary.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
