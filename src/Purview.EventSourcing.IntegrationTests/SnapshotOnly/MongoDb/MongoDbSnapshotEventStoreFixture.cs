﻿namespace Purview.EventSourcing.SnapshotOnly.MongoDB;

public class MongoDBSnapshotEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.Azurite.AzuriteContainer _azuriteContainer;
	readonly Testcontainers.MongoDb.MongoDbContainer _mongoDBContainer;

	public MongoDBSnapshotEventStoreFixture()
	{
		_azuriteContainer = ContainerHelper.CreateAzurite();
		_mongoDBContainer = ContainerHelper.CreateMongoDB();
	}

	public MongoDBSnapshotTestContext CreateContext(int correlationIdsToGenerate = 1, string? collectionName = null)
		=> new(_mongoDBContainer.GetConnectionString(), _azuriteContainer.GetConnectionString(), correlationIdsToGenerate, collectionName);

	public async Task InitializeAsync()
	{
		await _mongoDBContainer.StartAsync();
		await _azuriteContainer.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _mongoDBContainer.DisposeAsync();
		await _azuriteContainer.DisposeAsync();
	}
}
