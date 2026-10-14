using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Weather.Modules.Favorites.Application.Telemetry;

namespace Weather.Modules.Favorites.Infrastructure.Database;

/// <summary>Applies the Favorites migrations when the host starts.</summary>
internal sealed class FavoritesDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        FavoritesContext context = scope.ServiceProvider.GetRequiredService<FavoritesContext>();

        await context.Database.MigrateAsync(cancellationToken);
        FavoritesMetrics.SetSavedCount(await context.FavoriteCities.CountAsync(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
