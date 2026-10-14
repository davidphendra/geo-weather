using FluentValidation;

using global::Weather.BuildingBlocks.Application.Configuration;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Weather.Modules.Weather.Infrastructure;

public static class WeatherModuleExtensions
{
    public static IServiceCollection AddWeatherModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenWeatherOptions>()
            .Bind(configuration.GetSection(OpenWeatherOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<OpenWeatherApiKeyHandler>();

        services.AddHttpClient<IWeatherProvider, OpenWeatherMapService>((serviceProvider, httpClient) =>
            {
                OpenWeatherOptions options = serviceProvider.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            })
            .AddHttpMessageHandler<OpenWeatherApiKeyHandler>()
            .AddStandardResilienceHandler();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(IWeatherProvider).Assembly));
        services.AddValidatorsFromAssembly(typeof(IWeatherProvider).Assembly, includeInternalTypes: true);
        services.AddWeatherValidationBehavior();
        services.AddScoped<IWeatherService, WeatherService>();

        return services;
    }
}
