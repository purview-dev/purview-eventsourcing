using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Outbox;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the transactional outbox dispatcher and its dependencies.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
	/// <summary>
	/// Registers an outbox store, an outbox handler, the lease-protected dispatcher, and a hosted
	/// dispatch loop.
	/// </summary>
	/// <typeparam name="TOutboxStore">The <see cref="IOutboxStore"/> implementation.</typeparam>
	/// <typeparam name="TOutboxHandler">The <see cref="IOutboxHandler"/> implementation.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Optional <see cref="OutboxDispatchOptions"/> configuration.</param>
	/// <returns>The <paramref name="services"/> for chaining.</returns>
	/// <remarks>
	/// An outbox guarantees atomic persistence plus at-least-once delivery: the handler must be
	/// idempotent because a message can be delivered more than once.
	/// </remarks>
	public static IServiceCollection AddOutbox<TOutboxStore, TOutboxHandler>(
		this IServiceCollection services,
		Action<OutboxDispatchOptions>? configure = null
	)
		where TOutboxStore : class, IOutboxStore
		where TOutboxHandler : class, IOutboxHandler
	{
		ArgumentNullException.ThrowIfNull(services);

		services.TryAddSingleton<TOutboxStore>();
		services.TryAddSingleton<IOutboxStore>(static services => services.GetRequiredService<TOutboxStore>());
		services.TryAddSingleton<TOutboxHandler>();
		services.TryAddSingleton<IOutboxHandler>(static services => services.GetRequiredService<TOutboxHandler>());
		services.TryAddSingleton<IOutboxDispatcher, OutboxDispatcher>();
		services.AddHostedService<OutboxDispatchHostedService>();

		services
			.AddOptions<OutboxDispatchOptions>()
			.Configure(options => configure?.Invoke(options))
			.Validate(
				static options =>
					options.BatchSize >= 1
					&& options.MaxAttempts >= 1
					&& options.LeaseDuration > TimeSpan.Zero
					&& options.RetryBackoffBase >= TimeSpan.Zero
					&& options.PollInterval > TimeSpan.Zero
					&& options.Retention >= TimeSpan.Zero,
				"OutboxDispatchOptions is invalid."
			);

		return services;
	}
}
