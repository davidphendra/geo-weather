namespace Weather.WebUI.Services;

/// <summary>What a command dispatch mutates.</summary>
public enum MutationIntent
{
    /// <summary>Save the target city as a favorite.</summary>
    Favorite,

    /// <summary>Save a weather note for the target city.</summary>
    Note,
}
