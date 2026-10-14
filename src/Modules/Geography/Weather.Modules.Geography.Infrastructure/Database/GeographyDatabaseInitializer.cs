using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Weather.Modules.Geography.Infrastructure.Database;

/// <summary>Applies the Geography migrations (schema + seed) when the host starts.</summary>
internal sealed class GeographyDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<GeographyContext>()
            .Database
            .MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
