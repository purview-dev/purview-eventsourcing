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
	public static IServiceCollection AddAzureTableEventStore(this IServiceCollection services, IConfiguration configuration)
	{
		services
			//.AddAzureTableStorageClient()
			//.AddAzureBlobStorageClient()
			.AddEventSourcing();

		services
			.AddTransient(typeof(INonQueryableEventStore<>), typeof(TableEventStore<>))
			.AddTransient(typeof(ITableEventStore<>), typeof(TableEventStore<>))
			.AddTableEventStoreTelemetry();

		services.Configure<AzureStorageEventStoreOptions>(options =>
		{
			configuration.GetSection(AzureStorageEventStoreOptions.AzureStorageEventStore).Bind(options);

			if (options.ConnectionString == null)
				options.ConnectionString = configuration.GetConnectionString("EventStore_AzureStorage") ?? configuration.GetConnectionString("AzureStorage");
		});

		return services;
	}
}
