﻿using Newtonsoft.Json;
using Purview.EventSourcing.Aggregates.Events;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class TableEventStore<T>
{
	static IEvent? DeserializeEvent(string eventContent, Type eventType)
		=> JsonConvert.DeserializeObject(eventContent, eventType, JsonHelpers.JsonSerializerSettings) as IEvent;

	static string SerializeSnapshot(T aggregate)
		=> JsonConvert.SerializeObject(aggregate, aggregate.GetType(), JsonHelpers.JsonSerializerSettings);

	static string SerializeEvent(IEvent @event)
		=> JsonConvert.SerializeObject(@event, @event.GetType(), JsonHelpers.JsonSerializerSettings);

	static T DeserializeSnapshot(string aggregateContent)
		=> JsonConvert.DeserializeObject<T>(aggregateContent, JsonHelpers.JsonSerializerSettings)!;
}
