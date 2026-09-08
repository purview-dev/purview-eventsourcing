using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Outbox;

/// <summary>
/// Background dispatcher that polls the outbox on a fixed interval. The scope is created per cycle so
/// scoped outbox services (for example a scoped store backed by a connection factory) resolve correctly.
/// </summary>
public sealed class OutboxDispatchHostedService(
	IServiceScopeFactory scopeFactory,
	IOptions<OutboxDispatchOptions> options,
	ILogger<OutboxDispatchHostedService> logger
) : BackgroundService
{
	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = scopeFactory.CreateScope();
				var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
				await dispatcher.DispatchAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			// A store or dispatcher failure must not terminate the loop; the next cycle retries.
#pragma warning disable CA1031
			catch (Exception ex)
#pragma warning restore CA1031
			{
				logger.LogError(ex, "Outbox dispatch cycle failed; the loop will retry after the poll interval.");
			}

			await Task.Delay(options.Value.PollInterval, stoppingToken);
		}
	}
}
