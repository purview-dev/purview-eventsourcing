using System.ComponentModel;
using Microsoft.Extensions.Caching.Distributed;

namespace Purview.EventSourcing;

/// <summary>
/// Controls how and when the <see cref="IDistributedCache"/> is used, and how
/// snapshots are cached during save operations.
/// </summary>
/// <seealso cref="DistributedCacheEntryOptions"/>
[System.Diagnostics.DebuggerStepThrough]
public class EventStoreCacheOptions : DistributedCacheEntryOptions
{
	/// <summary>
	/// Gets/ sets a value indicating how the <see cref="IEventStore{T}"/>
	/// uses the <see cref="IDistributedCache"/> during it's operations. Defaults to <see cref="EventStoreCachingOptions.GetAndStore"/>.
	/// </summary>
	[DefaultValue(EventStoreCachingOptions.GetAndStore)]
	public EventStoreCachingOptions Mode { get; set; } = EventStoreCachingOptions.GetAndStore;
}
