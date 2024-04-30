﻿namespace Purview.EventSourcing.SnapshotOnly.CosmosDb;

public class CosmosDbSnapshotEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.Azurite.AzuriteContainer _azuriteContainer;
	readonly Testcontainers.CosmosDb.CosmosDbContainer _cosmosDbContainer;

	public CosmosDbSnapshotEventStoreFixture()
	{
		_azuriteContainer = ContainerHelper.CreateAzurite();
		_cosmosDbContainer = ContainerHelper.CreateCosmosDb();
	}

	public CosmosDbSnapshotEventStoreContext CreateContext(int correlationIdsToGenerate = 1)
	{
		CosmosDbSnapshotEventStoreContext eventStoreContext = new(
			_cosmosDbContainer.GetConnectionString(),
			_cosmosDbContainer.HttpClient,
			_azuriteContainer.GetConnectionString());

		eventStoreContext.CreateCosmosDbEventStore(correlationIdsToGenerate: correlationIdsToGenerate);

		return eventStoreContext;
	}

	public async Task DisposeAsync()
	{
		await _azuriteContainer.DisposeAsync().AsTask();
		await _cosmosDbContainer.DisposeAsync().AsTask();
	}

	public async Task InitializeAsync()
	{
		await _azuriteContainer.StartAsync();
		await _cosmosDbContainer.StartAsync();
	}
}
