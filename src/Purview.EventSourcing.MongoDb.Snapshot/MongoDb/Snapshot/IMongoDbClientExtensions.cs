﻿using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Purview.EventSourcing.MongoDb.Snapshot;

[EditorBrowsable(EditorBrowsableState.Never)]
static class IMongoDbClientExtensions
{
	#region GetQueryEnumerableAsync

	public static async IAsyncEnumerable<T> GetQueryEnumerableAsync<T>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

	public static IAsyncEnumerable<T> GetQueryEnumerableAsync<T>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.GetQueryEnumerableAsync(whereClause, null, maxRecordsPerOperation, cancellationToken);

	public static IAsyncEnumerable<T> GetQueryEnumerableAsync<T, TOrderBy>(this MongoDbClient mongoDbClient, Expression<Func<T, bool>> whereClause, Expression<Func<T, TOrderBy>> orderByAscending, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.GetQueryEnumerableAsync(whereClause, m => m.OrderBy(orderByAscending), maxRecordsPerOperation, cancellationToken);

	#endregion GetQueryEnumerableAsync

	#region GetListEnumerableAsync

	public static async IAsyncEnumerable<T> GetListEnumerableAsync<T>(this MongoDbClient mongoDbClient, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		where T : class
	{
		var request = new ContinuationRequest { MaxRecords = maxRecordsPerOperation };
		do
		{
			var response = await mongoDbClient.ListAsync(orderByClause, request, cancellationToken);
			foreach (var result in response.Results)
				yield return result;

			request = response.ToRequest();
		}
		while (request.ContinuationToken != null);
	}

	public static IAsyncEnumerable<T> GetListEnumerableAsync<T>(this MongoDbClient mongoDbClient, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.GetListEnumerableAsync<T>(null, maxRecordsPerOperation, cancellationToken);

	public static IAsyncEnumerable<T> GetListEnumerableAsync<T, TOrderBy>(this MongoDbClient mongoDbClient, Expression<Func<T, TOrderBy>> orderByAscending, int maxRecordsPerOperation = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> mongoDbClient.GetListEnumerableAsync<T>(m => m.OrderBy(orderByAscending), maxRecordsPerOperation, cancellationToken);

	#endregion GetListEnumerableAsync

	#region QueryAsync

	public static Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, null, maxRecords, cancellationToken);

	public static Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, ContinuationRequest request, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, null, request, cancellationToken);

	public static Task<ContinuationResponse<T>> QueryAsync<T>(this MongoDbClient client, Expression<Func<T, bool>> whereClause, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.QueryAsync(whereClause, orderByClause, new ContinuationRequest { MaxRecords = maxRecords }, cancellationToken);

	#endregion QueryAsync

	#region ListAsync

	public static Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync<T>(null, maxRecords, cancellationToken);

	public static Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, ContinuationRequest request, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync<T>(null, request, cancellationToken);

	public static Task<ContinuationResponse<T>> ListAsync<T>(this MongoDbClient client, Func<IQueryable<T>, IQueryable<T>>? orderByClause, int maxRecords = ContinuationRequest.DefaultMaxRecords, CancellationToken cancellationToken = default)
		where T : class
		=> client.ListAsync(orderByClause, new ContinuationRequest { MaxRecords = maxRecords }, cancellationToken);

	#endregion ListAsync

	public static Task<long> CountAsync<T>(this MongoDbClient client, CancellationToken cancellationToken = default)
		where T : class
		=> client.CountAsync<T>(null, cancellationToken);
}
