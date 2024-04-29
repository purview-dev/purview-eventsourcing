using MongoDB.Driver;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.MongoDb;

namespace Purview.EventSourcing.MongoDb.Snapshot;

[Collection("MongoDb")]
[NCrunch.Framework.Category("MongoDb")]
[NCrunch.Framework.Category("Storage")]
public partial class MongoDbSnapshotEventStoreTests(MongoDbSnapshotEventStoreFixture fixture) : IClassFixture<MongoDbSnapshotEventStoreFixture>
{
	static PersistenceAggregate CreateAggregate(string? id = null, Action<PersistenceAggregate>? action = null)
	{
		PersistenceAggregate aggregate = new()
		{
			Details =
			{
				Id = id ?? Guid.NewGuid().ToString()
			}
		};

		action?.Invoke(aggregate);

		return aggregate;
	}

	static FilterDefinition<PersistenceAggregate> PredicateId(string aggregateId)
	{
		var builder = new FilterDefinitionBuilder<PersistenceAggregate>()
			.Eq("_id", aggregateId);

		return builder;
	}
}
