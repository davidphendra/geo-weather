using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Weather.WebUI.Api.ErrorHandling;

namespace Weather.WebUI.Hosting;

/// <summary>Wires the RFC 9457 error handling shared by the REST API.</summary>
internal static class ApiErrorHandlingExtensions
{
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ProblemDetailsCustomization.Customize);
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = ApiBehaviorCustomization.InvalidModelStateResponse);

        return services;
    }
}
