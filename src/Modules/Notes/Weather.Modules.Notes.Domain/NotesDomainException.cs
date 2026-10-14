namespace Weather.Modules.Notes.Domain;

/// <summary>Thrown when a Notes domain invariant is violated.</summary>
public sealed class NotesDomainException : Exception
{
    public NotesDomainException(string message) : base(message)
    {
    }
}
