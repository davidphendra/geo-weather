using FluentValidation;

using global::Weather.BuildingBlocks.Application.Configuration;
using global::Weather.Modules.Weather.Application.Abstractions;
using global::Weather.Modules.Weather.Application.Contracts;
using global::Weather.Modules.Weather.Infrastructure.OpenWeatherMap;

using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using Polly;

namespace Weather.Modules.Weather.Infrastructure;

public static class WeatherModuleExtensions
{
    private const string OpenWeatherPipelineName = "openweather";

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

        services.AddSingleton<IValidateOptions<OpenWeatherOptions>, OpenWeatherOptionsValidator>();

        services.AddOptions<OpenWeatherResilienceOptions>()
            .Bind(configuration.GetSection(OpenWeatherResilienceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OpenWeatherResilienceOptions>, OpenWeatherResilienceOptionsValidator>();

        services.AddTransient<OpenWeatherApiKeyHandler>();

        services.AddHttpClient<IWeatherProvider, OpenWeatherMapService>((serviceProvider, httpClient) =>
            {
                OpenWeatherOptions options = serviceProvider.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
                httpClient.BaseAddress = options.BaseUrl;
            })
            .AddHttpMessageHandler<OpenWeatherApiKeyHandler>()
            .AddResilienceHandler(OpenWeatherPipelineName, (builder, context) =>
            {
                OpenWeatherResilienceOptions resilience = context.ServiceProvider
                    .GetRequiredService<IOptions<OpenWeatherResilienceOptions>>()
                    .Value;

                // Total request timeout (outermost) caps the whole retry budget.
                builder.AddTimeout(TimeSpan.FromSeconds(resilience.TotalTimeoutSeconds));

                // Polly requires MaxRetryAttempts >= 1, so 0 means "no retry strategy" (single attempt).
                if (resilience.MaxRetryAttempts > 0)
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = resilience.MaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        Delay = TimeSpan.FromSeconds(resilience.RetryBaseDelaySeconds),
                        ShouldRetryAfterHeader = true,
                    });
                }

                // Attempt timeout (innermost) bounds each individual call.
                builder.AddTimeout(TimeSpan.FromSeconds(resilience.AttemptTimeoutSeconds));
            });

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(IWeatherProvider).Assembly));
        services.AddValidatorsFromAssembly(typeof(IWeatherProvider).Assembly, includeInternalTypes: true);
        services.AddWeatherValidationBehavior();
        services.AddScoped<IWeatherService, WeatherService>();

        return services;
    }
}
