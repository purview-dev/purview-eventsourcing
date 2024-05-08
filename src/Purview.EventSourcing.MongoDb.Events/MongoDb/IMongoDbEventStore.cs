using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Internal;

namespace Purview.EventSourcing.MongoDb;

public interface IMongoDbEventStore<T> : INonQueryableEventStore<T>
	where T : class, IAggregate, new()
{
}
