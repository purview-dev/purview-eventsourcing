using System.Collections.Immutable;

namespace Purview.EventSourcing;

/// <summary>
/// Provides the effective <see cref="EventStoreCapabilities"/> for the registered event stores,
/// resolved without constructing a store or probing live storage.
/// </summary>
public interface IEventStoreCapabilitiesProvider
{
	/// <summary>
	/// Gets the merged capability contract for all registered event stores.
	/// </summary>
	EventStoreCapabilities GetCapabilities();
}

/// <summary>
/// Default <see cref="IEventStoreCapabilitiesProvider"/> that merges every registered
/// <see cref="EventStoreCapabilities"/> part. When no provider registers capabilities, the
/// conservative <see cref="EventStoreCapabilities.Default"/> is reported.
/// </summary>
public sealed class EventStoreCapabilitiesProvider(IEnumerable<EventStoreCapabilities> parts)
	: IEventStoreCapabilitiesProvider
{
	readonly ImmutableArray<EventStoreCapabilities> _parts = [.. parts];

	/// <inheritdoc/>
	public EventStoreCapabilities GetCapabilities() =>
		_parts.IsEmpty ? EventStoreCapabilities.Default : EventStoreCapabilities.Merge(_parts);
}
