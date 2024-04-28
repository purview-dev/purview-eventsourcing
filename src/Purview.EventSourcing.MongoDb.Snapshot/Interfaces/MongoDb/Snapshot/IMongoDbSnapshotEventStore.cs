using Purview.EventSourcing.Interfaces.Aggregates;

namespace Purview.EventSourcing.Interfaces.MongoDb;

public interface IMongoDbSnapshotEventStore<T> : IQueryableEventStore<T>
	where T : class, IAggregate, new()
{
	Task ForceSaveAsync(T aggregate, CancellationToken cancellationToken = default);
}
