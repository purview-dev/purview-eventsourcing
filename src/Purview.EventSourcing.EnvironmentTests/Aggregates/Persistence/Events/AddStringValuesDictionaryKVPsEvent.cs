using Microsoft.Extensions.Primitives;
using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.Aggregates.Persistence.Events;

public class AddStringValuesDictionaryKVPsEvent : EventBase
{
	public KeyValuePair<string, StringValues>[] KVPs { get; set; } = default!;

	protected override void BuildEventHash(ref HashCode hash)
	{
		foreach (var kvp in KVPs)
		{
			hash.Add(kvp.Key);
			hash.Add(kvp.Value);
		}
	}
}
