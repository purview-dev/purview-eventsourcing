using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDb.Snapshot;

public interface IMongoDbSnapshotEventStore<T> : IQueryableEventStore<T>
	where T : class, IAggregate, new()
{
	Task ForceSaveAsync(T aggregate, CancellationToken cancellationToken = default);
}
