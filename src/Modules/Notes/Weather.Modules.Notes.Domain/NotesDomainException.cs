using Weather.BuildingBlocks.Domain;

namespace Weather.Modules.Notes.Domain;

/// <summary>Thrown when a Notes domain invariant is violated.</summary>
public sealed class NotesDomainException : DomainException
{
    public NotesDomainException(string message) : base(message)
    {
    }
}
