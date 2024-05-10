using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Purview.EventSourcing.MongoDB.Entities;

namespace Purview.EventSourcing.MongoDB.StorageClients;

partial class MongoDBClient
{
	public async Task SubmitBatchAsync(BatchOperation operation, CancellationToken cancellationToken = default)
	{
		var collection = GetCollection<IEntity>().WithWriteConcern(WriteConcern.WMajority);
		using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);

		TransactionOptions transactionOptions = new(writeConcern: WriteConcern.WMajority);
		await session.WithTransactionAsync(async (s, ct) =>
		{
			try
			{
				foreach (var op in operation.GetActions())
				{
					switch (op.ActionType)
					{
						case TransactionActionType.Add:
							await collection.InsertOneAsync(session, op.Document, cancellationToken: cancellationToken);
							break;
						case TransactionActionType.Update:
							await collection.ReplaceOneAsync(session, BuildPredicate<IEntity>(op.Document.Id, op.Document.EntityType), op.Document, new ReplaceOptions() { IsUpsert = false }, cancellationToken: cancellationToken);
							break;
						case TransactionActionType.Delete:
							await collection.DeleteOneAsync(session, BuildPredicate<IEntity>(op.Document.Id, op.Document.EntityType), cancellationToken: cancellationToken);
							break;
					}
				}
			}
			catch (MongoWriteException)
			{
				// Do something in response to the exception
				throw; // NOTE: You must rethrow the exception otherwise an infinite loop can occur.
			}

			return true;
		},
		   transactionOptions,
			cancellationToken
		);
	}

	public async Task SubmitDeleteBatchAsync(IEnumerable<string> entityIds, CancellationToken cancellationToken = default)
	{
		var collection = GetCollection<BsonDocument>().WithWriteConcern(WriteConcern.WMajority);
		using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);

		TransactionOptions transactionOptions = new(writeConcern: WriteConcern.WMajority);
		await session.WithTransactionAsync(async (s, ct) =>
		{
			try
			{
				foreach (var id in entityIds)
					await collection.DeleteOneAsync(session, BuildPredicate<BsonDocument>(id, null), cancellationToken: cancellationToken);
			}
			catch (MongoWriteException)
			{
				// Do something in response to the exception
				throw; // NOTE: You must rethrow the exception otherwise an infinite loop can occur.
			}

			return true;
		},
		   transactionOptions,
			cancellationToken
		);
	}

	public async Task<T?> GetAsync<T>(FilterDefinition<T> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();
		var findResult = await collection.FindAsync(predicate, new FindOptions<T, T> { Limit = 1 }, cancellationToken);

		return await findResult.SingleOrDefaultAsync(cancellationToken);
	}

	public Task<T?> GetAsync<T>(string id, int? entityType, CancellationToken cancellationToken = default)
		where T : class
		=> GetAsync(BuildPredicate<T>(id, entityType), cancellationToken);

	public async Task<T?> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return await collection.AsQueryable().Take(1).SingleOrDefaultAsync(predicate, cancellationToken);
	}

	async public Task<bool> UpsertAsync<T>(T document, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		var result = await collection.ReplaceOneAsync(predicate, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
		return result.IsAcknowledged;
	}

	async public Task<bool> UpsertAsync<T>(T document, FilterDefinition<T> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		var result = await collection.ReplaceOneAsync(predicate, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
		return result.IsAcknowledged;
	}

	public Task InsertAsync<T>(T document, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.InsertOneAsync(document, null, cancellationToken);
	}

	public Task InsertAsync<T>(T[] documents, CancellationToken cancellationToken = default)
		where T : class
	{
		return InsertAsync(documents.AsEnumerable(), cancellationToken);
	}

	public Task InsertAsync<T>(IEnumerable<T> documents, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.InsertManyAsync(documents, null, cancellationToken);
	}

	public Task DeleteAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.DeleteOneAsync(predicate, cancellationToken);
	}

	public Task DeleteAsync<T>(FilterDefinition<T> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.DeleteOneAsync(predicate, cancellationToken);
	}

	public Task DeleteManyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.DeleteManyAsync(predicate, cancellationToken);
	}

	public Task DeleteDatabaseAsync(CancellationToken cancellationToken = default)
		=> _client.DropDatabaseAsync(_configuration.Database, cancellationToken);

	public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
		=> _database.DropCollectionAsync(_collectionName, cancellationToken);

	IMongoCollection<T> GetCollection<T>()
		where T : class
		=> _database.GetCollection<T>(_collectionName);
}
