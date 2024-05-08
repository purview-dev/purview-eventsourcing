using MongoDB.Driver;

namespace Purview.EventSourcing.MongoDb.StorageClients;

sealed partial class MongoDbClient
{
	readonly MongoDbConfiguration _configuration;
	readonly MongoClient _client;
	readonly IMongoDatabase _database;

	readonly string _collectionName;

	public MongoDbClient(MongoDbConfiguration configuration, string? databaseOverride = null, string? collectionOverride = null)
	{
		_configuration = configuration;

		var settings = MongoClientSettings.FromConnectionString(configuration.ConnectionString);
		settings.ApplicationName = configuration.ApplicationName;

		_client = new MongoClient(settings);
		_database = _client.GetDatabase(databaseOverride ?? configuration.Database);
		_collectionName = collectionOverride ?? configuration.Collection;
	}
}
