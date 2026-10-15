using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Weather.Modules.Journal.Application.Telemetry;

namespace Weather.Modules.Journal.Infrastructure.Database;

/// <summary>Applies the Journal migrations when the host starts and seeds the business gauges.</summary>
internal sealed class JournalDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        JournalContext context = scope.ServiceProvider.GetRequiredService<JournalContext>();

        await context.Database.MigrateAsync(cancellationToken);
        FavoritesMetrics.SetSavedCount(await context.FavoriteCities.CountAsync(cancellationToken));
        NotesMetrics.SetSavedCount(await context.WeatherNotes.CountAsync(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
