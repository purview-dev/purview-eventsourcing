﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Purview.EventSourcing.MongoDB.Entities;

namespace Purview.EventSourcing.MongoDB.StorageClients;

sealed partial class MongoDBClient
{
	readonly IMongoDBClientTelemetry _telemetry;

	readonly MongoDBConfiguration _configuration;
	readonly MongoClient _client;
	readonly IMongoDatabase _database;

	readonly string _collectionName;

	static MongoDBClient()
	{
		try
		{
			BsonSerializer.RegisterSerializationProvider(new MongoDBAggregateSerializationProvider());

			var iEntityType = typeof(IEntity);

			BsonSerializer.RegisterSerializer(new ObjectSerializer(iEntityType.IsAssignableFrom));
		}
		catch { }
	}

	public MongoDBClient(IMongoDBClientTelemetry telemetry, MongoDBConfiguration configuration, string? databaseOverride = null, string? collectionOverride = null)
	{
		_telemetry = telemetry;
		_configuration = configuration;

		var settings = MongoClientSettings.FromConnectionString(configuration.ConnectionString);
		settings.ApplicationName = configuration.ApplicationName;

		_client = new MongoClient(settings);
		_database = _client.GetDatabase(databaseOverride ?? configuration.Database);
		_collectionName = collectionOverride ?? configuration.Collection;
	}

	static FilterDefinition<T> BuildPredicate<T>(string id, int? entityType)
	{
		var predicate = new FilterDefinitionBuilder<T>();
		predicate.Eq("_id", id);

		if (entityType == null)
			return predicate.Eq("_id", id);

		predicate.Eq("_id", id);

		return predicate.And(predicate.Eq(nameof(IEntity.EntityType), entityType));
	}

	sealed class StringObjectIdIdGeneratorConventionThatWorks : ConventionBase, IPostProcessingConvention
	{
		public void PostProcess(BsonClassMap classMap)
		{
			var idMemberMap = classMap.IdMemberMap;
			if (idMemberMap == null || idMemberMap.IdGenerator != null)
				return;

			if (idMemberMap.MemberType == typeof(string))
			{
				idMemberMap
					.SetIdGenerator(StringObjectIdGenerator.Instance)
					.SetSerializer(new StringSerializer(BsonType.ObjectId));
			}
		}
	}
}
