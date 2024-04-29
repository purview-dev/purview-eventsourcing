using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

namespace Purview.EventSourcing.MongoDb.Snapshot;

sealed partial class MongoDbClient
{
	readonly MongoDbEventStoreOptions _configuration;
	readonly MongoClient _client;
	readonly IMongoDatabase _database;

	readonly string _collectionName;

	public MongoDbClient([NotNull] MongoDbEventStoreOptions configuration, string? databaseOverride = null, string? collectionOverride = null)
	{
		_configuration = configuration;

		var settings = MongoClientSettings.FromConnectionString(_configuration.ConnectionString);
		settings.ApplicationName = _configuration.ApplicationName;

		_client = new MongoClient(settings);
		_database = _client.GetDatabase(databaseOverride ?? _configuration.Database);
		_collectionName = collectionOverride ?? _configuration.Collection;
	}
}
