using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Weather.Modules.Notes.Infrastructure.Database;

/// <summary>Applies the Notes migrations when the host starts.</summary>
internal sealed class NotesDatabaseInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<NotesContext>().Database.Migrate();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
