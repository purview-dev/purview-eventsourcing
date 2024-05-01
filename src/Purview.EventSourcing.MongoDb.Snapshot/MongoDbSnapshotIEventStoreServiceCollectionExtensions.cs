using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.MongoDb.Snapshot;

namespace Purview;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MongoDbSnapshotIEventStoreServiceCollectionExtensions
{
	public static IServiceCollection AddMongoDbSnapshotQueryableEventStore(this IServiceCollection services, bool registerAsIEventStore = false)
	{
		services.AddEventSourcing();

		services
			.AddTransient(typeof(IQueryableEventStore<>), typeof(MongoDbSnapshotEventStore<>))
			.AddTransient(typeof(IMongoDbSnapshotEventStore<>), typeof(MongoDbSnapshotEventStore<>))
			.AddMongoDbSnapshotEventStoreTelemetry();

		if (registerAsIEventStore)
			services.AddTransient(typeof(IEventStore<>), typeof(MongoDbSnapshotEventStore<>));

		services
			.AddOptions<MongoDbEventStoreOptions>()
			.Configure<IConfiguration>((options, configuration) =>
			{
				configuration.GetSection(MongoDbEventStoreOptions.MongoDbEventStore).Bind(options);

				if (options.ConnectionString == null)
				{
					options.ConnectionString =
						configuration.GetConnectionString("EventStore_MongoDb")
						?? configuration.GetConnectionString("MongoDb")
						// This will get picked up by the validation.
						?? default!;
				}
			})
			.ValidateOnStart();

		return services;
	}
}
