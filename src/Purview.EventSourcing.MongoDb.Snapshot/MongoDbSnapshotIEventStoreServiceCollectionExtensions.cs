using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.MongoDb.Snapshot;

namespace Purview;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MongoDbSnapshotIEventStoreServiceCollectionExtensions
{
	public static IServiceCollection AddMongoDbSnapshotQueryableEventStore(this IServiceCollection services)
	{
		return services.AddModule<MongoDbSnapshotEventSourcingModule>();
	}
}
