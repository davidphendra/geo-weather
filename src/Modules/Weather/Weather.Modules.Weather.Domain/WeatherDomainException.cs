namespace Weather.Modules.Weather.Domain;

/// <summary>Thrown when a Weather domain invariant is violated.</summary>
public sealed class WeatherDomainException : Exception
{
    public WeatherDomainException(string message) : base(message)
    {
    }
}
