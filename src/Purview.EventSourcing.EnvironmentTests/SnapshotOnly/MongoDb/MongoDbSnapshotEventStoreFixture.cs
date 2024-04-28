using Purview.Testing;

namespace Purview.EventSourcing.SnapshotOnly.MongoDb;

public class MongoDbSnapshotEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.Azurite.AzuriteContainer _azuriteContainer;
	readonly Testcontainers.MongoDb.MongoDbContainer _mongoDbContainer;

	public MongoDbSnapshotEventStoreFixture()
	{
		_azuriteContainer = ContainerHelper.CreateAzurite();
		_mongoDbContainer = ContainerHelper.CreateMongoDb();
	}

	public MongoDbSnapshotTestContext CreateContext(int correlationIdsToGenerate = 1, string? collectionName = null)
		=> new(_mongoDbContainer.GetConnectionString(), _azuriteContainer.GetConnectionString(), correlationIdsToGenerate, collectionName);

	public async Task InitializeAsync()
	{
		await _mongoDbContainer.StartAsync();
		await _azuriteContainer.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _mongoDbContainer.DisposeAsync();
		await _azuriteContainer.DisposeAsync();
	}
}
