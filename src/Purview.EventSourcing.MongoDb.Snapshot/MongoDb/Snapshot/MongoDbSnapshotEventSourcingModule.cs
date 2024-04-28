using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Interfaces;
using Purview.EventSourcing.Interfaces.MongoDb;
using Purview.EventSourcing.MongoDb.Snapshot.Options;

namespace Purview.EventSourcing.MongoDb.Snapshot;

public class MongoDbSnapshotEventSourcingModule : ModuleBase
{
	public override void Configure(IServiceCollection services)
	{
		services
			.AddMongoDbStorageClient()
			.AddEventSourcing();

		services
			.AddScoped(typeof(IQueryableEventStore<>), typeof(MongoDbSnapshotEventStore<>))
			.AddScoped(typeof(IEventStore<>), typeof(MongoDbSnapshotEventStore<>))
			.AddTransient(typeof(IMongoDbSnapshotEventStore<>), typeof(MongoDbSnapshotEventStore<>));

		services.RegisterOptionsType<MongoDbEventStoreConfiguration>(Configuration);
	}
}
