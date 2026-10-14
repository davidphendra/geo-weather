namespace Weather.WebUI.Services;

/// <summary>The record-classification filter on the Favorites page.</summary>
public enum EntityTypeFilter
{
    /// <summary>Everything the API can list (currently favorites only).</summary>
    All,

    /// <summary>Favorite cities only.</summary>
    Favorites,
}
