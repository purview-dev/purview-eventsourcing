using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.AzureStorage.Table;
using Purview.EventSourcing.AzureStorage.Table.Options;
using Purview.EventSourcing.Internal;

namespace Purview.EventSourcing;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class AzureStorageEventStoreIServiceCollectionExtensions
{
	public static IServiceCollection AddAzureTableEventStore(this IServiceCollection services)
	{
		services.AddEventSourcing();

		services
			.AddTransient(typeof(IEventStore<>), typeof(TableEventStore<>))
			.AddTransient(typeof(INonQueryableEventStore<>), typeof(TableEventStore<>))
			.AddTransient(typeof(ITableEventStore<>), typeof(TableEventStore<>))
			.AddTableEventStoreTelemetry();

		services
			.AddOptions<AzureStorageEventStoreOptions>()
			.Configure<IConfiguration>((options, configuration) =>
			{
				configuration.GetSection(AzureStorageEventStoreOptions.AzureStorageEventStore).Bind(options);

				if (options.ConnectionString == null)
					options.ConnectionString = configuration.GetConnectionString("EventStore_AzureStorage") ?? configuration.GetConnectionString("AzureStorage");
			})
			.ValidateOnStart();

		return services;
	}
}
