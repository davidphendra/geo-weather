using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Weather.BuildingBlocks.Application.Configuration;

public static class ValidationBehaviorRegistration
{
    /// <summary>Registers the shared validation pipeline for the module's MediatR handlers.</summary>
    public static IServiceCollection AddWeatherValidationBehavior(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
