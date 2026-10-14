namespace Weather.WebUI.Services;

/// <summary>One client-observed command in the CQRS mutation feed.</summary>
public sealed record CommandLogEntry(DateTimeOffset At, MutationIntent Intent, CommandOutcome Outcome);
