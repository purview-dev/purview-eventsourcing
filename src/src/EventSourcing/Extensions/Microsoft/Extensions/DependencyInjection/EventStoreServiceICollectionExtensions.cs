using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Aggregates.Events;
using Purview.EventSourcing.Aggregates.Events.Upcasting;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.EventStores.NullQueryable;
using Purview.EventSourcing.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the Purview EventSourcing framework services and optional store implementations.
/// </summary>
/// <remarks>
/// These extension methods register the core services such as the event-name mapper, aggregate requirements
/// manager, change-feed notifier, upcaster registry, correlation ID provider, and transaction factory. They
/// are hidden from IntelliSense because they are invoked through the <see cref="IServiceCollection"/>.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.DebuggerStepThrough]
public static class EventStoreServiceICollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Register the event store components.
		/// </summary>
		/// <returns>The <paramref name="services"/> passed in.</returns>
		public IServiceCollection AddEventSourcing()
		{
			services
				.AddSingleton<IAggregateEventNameMapper, AggregateEventNameMapper>()
				.AddAggregateChangeFeedNotifierTelemetry()
				.AddScoped<IAggregateRequirementsManager, AggregateRequiredServiceManager>()
				.AddScoped(typeof(IAggregateChangeFeedNotifier<>), typeof(AggregateChangeFeedNotifier<>))
				.AddSingleton<IEventUpcasterRegistry, EventUpcasterRegistry>();

			services.TryAddSingleton<IEventStoreCorrelationIdProvider, ActivityEventStoreCorrelationIdProvider>();
			services.TryAddSingleton<IEventStoreTransactionFactory, EventStoreTransactionFactory>();
			services.TryAddSingleton<IAggregateTypeRegistry, AssemblyAggregateTypeRegistry>();

			// Capability discovery is always available and reports the conservative default until a
			// provider registers its truthful capabilities.
			services.TryAddSingleton<IEventStoreCapabilitiesProvider, EventStoreCapabilitiesProvider>();

			return services;
		}

		/// <summary>
		/// Registers the event-store capabilities that a provider actually implements. Custom providers
		/// must not claim stronger behavior than they provide; <see cref="EventStoreCapabilities.Default"/>
		/// is the conservative baseline used when nothing is registered.
		/// </summary>
		/// <param name="capabilities">The truthful capabilities of the registered store.</param>
		/// <returns>The <paramref name="services"/> passed in.</returns>
		public IServiceCollection AddEventStoreCapabilities(EventStoreCapabilities capabilities)
		{
			ArgumentNullException.ThrowIfNull(capabilities);

			services.TryAddSingleton<IEventStoreCapabilitiesProvider, EventStoreCapabilitiesProvider>();
			services.AddSingleton(capabilities);

			return services;
		}

		/// <summary>
		/// Registers a null (non-persisting) queryable event store and its facade, useful for development and
		/// testing scenarios.
		/// </summary>
		/// <returns>The <paramref name="services"/> passed in.</returns>
		public IServiceCollection AddNullQueryableEventStore()
		{
			services
				.AddTransient(typeof(IQueryableEventStoreCore<>), typeof(NullQueryableEventStore<>))
				.TryAddTransient<IQueryableEventStore, QueryableEventStoreFacade>();

			return services;
		}

		/// <summary>
		/// Registers an <see cref="IEventUpcaster{TSource,TTarget}"/> so that legacy events stored
		/// under <typeparamref name="TSource"/> are automatically up-cast to
		/// <typeparamref name="TTarget"/> during event replay.
		/// </summary>
		/// <typeparam name="TSource">The legacy event type.</typeparam>
		/// <typeparam name="TTarget">The current event type.</typeparam>
		/// <typeparam name="TUpcaster">The upcaster implementation.</typeparam>
		/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
		/// <remarks>
		/// This method also registers a corresponding <see cref="IEventUpcasterDescriptor"/> so that
		/// the <see cref="IEventUpcasterRegistry"/> can discover the upcaster at runtime.
		/// <see cref="AddEventSourcing"/> must be called before (or after) this method on the same
		/// <paramref name="services"/> collection.
		/// </remarks>
		public IServiceCollection AddEventUpcaster<TSource, TTarget, TUpcaster>()
			where TSource : IEvent
			where TTarget : IEvent
			where TUpcaster : class, IEventUpcaster<TSource, TTarget>
		{
			services
				.AddSingleton<IEventUpcaster<TSource, TTarget>, TUpcaster>()
				.AddSingleton<IEventUpcasterDescriptor>(sp => new EventUpcasterDescriptor<TSource, TTarget>(
					sp.GetRequiredService<IEventUpcaster<TSource, TTarget>>()
				));

			return services;
		}
	}
}
