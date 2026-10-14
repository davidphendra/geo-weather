namespace Weather.Modules.Weather.Application.Abstractions;

/// <summary>Thrown when the external weather provider fails or returns invalid data.</summary>
public sealed class WeatherProviderException : Exception
{
    public WeatherProviderException(string message) : base(message)
    {
    }

    public WeatherProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
