using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Weather.WebUI.Security;

/// <summary>Registers and applies the Content Security Policy.</summary>
public static class CspServiceExtensions
{
    /// <summary>Binds <see cref="CspOptions"/> from configuration.</summary>
    public static IServiceCollection AddCsp(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CspOptions>(configuration.GetSection(CspOptions.SectionName));

        return services;
    }

    /// <summary>Adds the CSP middleware. Register before static files and endpoints so every response is covered.</summary>
    public static IApplicationBuilder UseCsp(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CspMiddleware>();
    }
}
