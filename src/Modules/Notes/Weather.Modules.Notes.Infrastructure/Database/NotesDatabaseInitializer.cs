using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Weather.Modules.Notes.Application.Telemetry;

namespace Weather.Modules.Notes.Infrastructure.Database;

/// <summary>Applies the Notes migrations when the host starts.</summary>
internal sealed class NotesDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        NotesContext context = scope.ServiceProvider.GetRequiredService<NotesContext>();

        await context.Database.MigrateAsync(cancellationToken);
        NotesMetrics.SetSavedCount(await context.WeatherNotes.CountAsync(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
