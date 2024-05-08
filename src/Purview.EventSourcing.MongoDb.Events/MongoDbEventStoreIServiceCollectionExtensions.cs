using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.Internal;
using Purview.EventSourcing.MongoDb;

namespace Purview.EventSourcing;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MongoDbEventStoreIServiceCollectionExtensions
{
	public static IServiceCollection AddMongoDbEventStore(this IServiceCollection services)
	{
		services.AddEventSourcing();

		services
			.AddTransient(typeof(IEventStore<>), typeof(MongoDbEventStore<>))
			.AddTransient(typeof(INonQueryableEventStore<>), typeof(MongoDbEventStore<>))
			.AddTransient(typeof(IMongoDbEventStore<>), typeof(MongoDbEventStore<>))
			.AddMongoDbEventStoreTelemetry();

		services
			.AddOptions<MongoDbEventStoreOptions>()
			.Configure<IConfiguration>((options, configuration) =>
			{
				configuration.GetSection(MongoDbEventStoreOptions.MongoDbEventStore).Bind(options);

				if (string.IsNullOrWhiteSpace(options.ConnectionString))
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
