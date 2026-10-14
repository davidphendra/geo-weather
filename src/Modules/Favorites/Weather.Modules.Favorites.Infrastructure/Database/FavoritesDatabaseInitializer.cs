using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Weather.Modules.Favorites.Infrastructure.Database;

/// <summary>Applies the Favorites migrations when the host starts.</summary>
internal sealed class FavoritesDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<FavoritesContext>()
            .Database
            .MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
