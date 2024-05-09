using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Purview.EventSourcing.MongoDb.StorageClients;

namespace Purview.EventSourcing.MongoDb;

[EditorBrowsable(EditorBrowsableState.Never)]
static class MongoDbClientExtensions
{
	#region QueryEnumerableAsync

	async static public IAsyncEnumerable<T> QueryEnumerableAsync<T>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		where T : class
	{
		var request = new ContinuationRequest { MaxRecords = maxRecordsPerOperation };
		do
		{
			var response = await mongoDbClient.QueryAsync(whereClause, orderByClause, request, cancellationToken);
			foreach (var result in response.Results)
				yield return result;

			request = response.ToRequest();
		}
		while (request.ContinuationToken != null);
	}

	static public IAsyncEnumerable<T> QueryEnumerableAsync<T>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.QueryEnumerableAsync(whereClause, null, maxRecordsPerOperation, cancellationToken);

	static public IAsyncEnumerable<T> QueryEnumerableAsync<T, TOrderBy>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, Expression<Func<T, TOrderBy>> orderByAscending, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.QueryEnumerableAsync(whereClause, m => m.OrderBy(orderByAscending), maxRecordsPerOperation, cancellationToken);

	#endregion QueryEnumerableAsync

	#region ListEnumerableAsync

	async static public IAsyncEnumerable<T> ListEnumerableAsync<T>(this MongoDbClient mongoDbClient, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		where T : class
	{
		ContinuationRequest request = new() { MaxRecords = maxRecordsPerOperation };
		do
		{
			var response = await mongoDbClient.ListAsync(orderByClause, request, cancellationToken);
			foreach (var result in response.Results)
				yield return result;

			request = response.ToRequest();
		}
		while (request.ContinuationToken != null);
	}

	static public IAsyncEnumerable<T> ListEnumerableAsync<T>(this MongoDbClient mongoDbClient, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.ListEnumerableAsync<T>(null, maxRecordsPerOperation, cancellationToken);

	static public IAsyncEnumerable<T> ListEnumerableAsync<T, TOrderBy>(this MongoDbClient mongoDbClient, Expression<Func<T, TOrderBy>> orderByAscending, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.ListEnumerableAsync<T>(m => m.OrderBy(orderByAscending), maxRecordsPerOperation, cancellationToken);

	#endregion ListEnumerableAsync

	#region QueryAsync

	static public Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, null, maxRecords, cancellationToken);

	static public Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, ContinuationRequest request, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, null, request, cancellationToken);

	static public Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, orderByClause, new ContinuationRequest { MaxRecords = maxRecords }, cancellationToken);

	#endregion QueryAsync

	#region ListAsync

	static public Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync<T>(null, maxRecords, cancellationToken);

	static public Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, ContinuationRequest request, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync<T>(null, request, cancellationToken);

	static public Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync(orderByClause, new ContinuationRequest { MaxRecords = maxRecords }, cancellationToken);

	#endregion ListAsync

	static public Task<long> CountAsync<T>(this MongoDbClient client, CancellationToken cancellationToken = default)
		where T : class
		=> client.CountAsync<T>(null, cancellationToken);
}
