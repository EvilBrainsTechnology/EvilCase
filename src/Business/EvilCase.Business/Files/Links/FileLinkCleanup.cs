using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Links;

/// <summary>
/// Removes expired links once per link lifetime. An expired link is refused whether or not it is removed yet.
/// </summary>
internal sealed class FileLinkCleanup(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<FileLinkCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FileLinkWriter.Lifetime, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var writer = scope.ServiceProvider.GetRequiredService<IFileLinkWriter>();
                await writer.DeleteExpiredFileLinks(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed round must not stop the host; the next one retries.
                logger.LogWarning(exception, "Expired file links were not removed");
            }
        }
    }
}
