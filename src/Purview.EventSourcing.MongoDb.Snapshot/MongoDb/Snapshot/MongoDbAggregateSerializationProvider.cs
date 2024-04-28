using System.Collections.Concurrent;
using MongoDB.Bson.Serialization;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Purview.EventSourcing.MongoDb.Snapshot;

internal class MongoDbAggregateSerializationProvider : IBsonSerializationProvider
{
	static readonly Type _aggregateInterfaceType = typeof(IAggregate);
	static readonly ConcurrentDictionary<Type, IBsonSerializer> _serializers = new();

	public IBsonSerializer GetSerializer(Type type)
	{
		return _aggregateInterfaceType.IsAssignableFrom(type)
			? GetOrCreateSerializer(type)
			: null!;
	}

	static IBsonSerializer GetOrCreateSerializer(Type type)
	{
		return _serializers.GetOrAdd(type, t =>
		{
			var serializerType = typeof(MongoDbAggregateSerializer<>).MakeGenericType(t);
			return (IBsonSerializer)Activator.CreateInstance(serializerType)!;
		});
	}
}
