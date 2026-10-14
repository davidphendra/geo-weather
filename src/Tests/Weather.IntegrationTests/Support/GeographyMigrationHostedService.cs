using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Weather.Modules.Geography.Infrastructure;

namespace Weather.IntegrationTests.Support;

/// <summary>Applies the Geography migrations (schema + seed) when the test host starts.</summary>
internal sealed class GeographyMigrationHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<GeographyContext>().Database.Migrate();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
