﻿using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purview.EventSourcing.CosmosDb.Snapshot;

namespace Purview;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class CosmosDbSnapshotIEventStoreServiceCollectionExtensions
{
	public static IServiceCollection AddCosmosDbQueryableEventStore(this IServiceCollection services)
	{
		services.AddEventSourcing();

		services
			.AddScoped(typeof(IQueryableEventStore<>), typeof(CosmosDbSnapshotEventStore<>))
			.AddScoped(typeof(IEventStore<>), typeof(CosmosDbSnapshotEventStore<>))
			.AddTransient(typeof(ICosmosDbSnapshotEventStore<>), typeof(CosmosDbSnapshotEventStore<>));

		services
			.AddOptions<CosmosDbEventStoreOptions>()
			.Configure<IConfiguration>((options, configuration) =>
			{
				configuration.GetSection(CosmosDbEventStoreOptions.CosmosDbEventStore).Bind(options);

				if (options.ConnectionString == null)
					options.ConnectionString = configuration.GetConnectionString("EventStore_CosmosDb") ?? configuration.GetConnectionString("CosmosDb");
			});

		return services;
	}
}
