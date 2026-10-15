using Weather.BuildingBlocks.Domain;

namespace Weather.Modules.Journal.Domain;

/// <summary>Thrown when a Favorites domain invariant is violated.</summary>
public sealed class JournalDomainException : DomainException
{
    public JournalDomainException(string message) : base(message)
    {
    }
}
