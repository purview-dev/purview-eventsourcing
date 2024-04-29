using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbClient
{
	static FilterDefinition<T> BuildPredicate<T>(string id)
		where T : class
	{
		var predicate = new FilterDefinitionBuilder<T>()
			.Eq("_id", id);

		return predicate;
	}

	public async Task<T?> GetAsync<T>(FilterDefinition<T> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();
		var findResult = await collection.FindAsync(predicate, new FindOptions<T, T> { Limit = 1 }, cancellationToken);

		return await findResult.SingleOrDefaultAsync(cancellationToken);
	}

	public Task<T?> GetAsync<T>(string id, CancellationToken cancellationToken = default)
		where T : class
		=> GetAsync(BuildPredicate<T>(id), cancellationToken);

	public async Task<T?> GetAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return await collection.AsQueryable().Take(1).SingleOrDefaultAsync(predicate, cancellationToken);
	}

	public Task UpsertAsync<T>(T document, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.ReplaceOneAsync(predicate, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
	}

	public Task UpsertAsync<T>(T document, FilterDefinition<T> predicate, CancellationToken cancellationToken = default)
		where T : class
	{
		var collection = GetCollection<T>();

		return collection.ReplaceOneAsync(predicate, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
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
