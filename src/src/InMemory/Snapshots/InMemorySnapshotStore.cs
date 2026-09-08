using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;
using Purview.EventSourcing.Validation;

namespace Purview.EventSourcing.InMemory.Snapshots;

/// <summary>
/// An in-memory <see cref="IQueryableEventStoreCore{T}"/> that persists aggregates and their events in
/// process and supports queryable snapshot reads.
/// </summary>
/// <typeparam name="T">An <see cref="IAggregate"/> implementation.</typeparam>
/// <param name="aggregateChangeNotifier">The notifier invoked before and after aggregates are saved or deleted.</param>
/// <param name="aggregateRequirementsManager">The manager used to fulfil aggregate requirements.</param>
/// <param name="validator">Optional <see cref="IAggregateValidator{T}"/> used to validate aggregates before they are saved.</param>
/// <param name="aggregateIdFactory">Optional factory used to generate aggregate ids when none is supplied.</param>
/// <remarks>
/// Queries are evaluated over the in-memory aggregates, so this store is intended for testing and
/// single-process scenarios; data is not shared between instances or persisted across restarts.
/// </remarks>
/// <seealso cref="IInMemorySnapshotStore{T}"/>
public sealed class InMemorySnapshotStore<T>(
	ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
	IAggregateRequirementsManager aggregateRequirementsManager,
	IAggregateValidator<T>? validator = null,
	IAggregateIdFactory? aggregateIdFactory = null
)
	: Events.InMemoryEventStore<T>(
		aggregateChangeNotifier,
		aggregateRequirementsManager,
		validator,
		aggregateIdFactory
	),
		IInMemorySnapshotStore<T>
	where T : class, IAggregate, new()
{
	///<inheritdoc/>
	public Task<long> CountAsync(
		Expression<Func<T, bool>>? whereClause,
		CancellationToken cancellationToken = default
	) => Task.FromResult(whereClause is null ? Aggregates.LongCount() : Aggregates.LongCount(whereClause.Compile()));

	///<inheritdoc/>
	public Task<T?> FirstOrDefaultAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (orderByClause != null)
			results = orderByClause(results.AsQueryable()).AsEnumerable();

		if (whereClause != null)
			results = results.Where(whereClause.Compile());

		return Task.FromResult(results.FirstOrDefault());
	}

	///<inheritdoc/>
	public IAsyncEnumerable<T> GetListEnumerableAsync(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = 20,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (orderByClause != null)
			results = orderByClause(results.AsQueryable()).AsEnumerable();

		return results.ToAsyncEnumerable();
	}

	///<inheritdoc/>
	public IAsyncEnumerable<T> GetQueryEnumerableAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		int maxRecordsPerIteration = 20,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (orderByClause != null)
			results = orderByClause(results.AsQueryable()).AsEnumerable();

		if (whereClause != null)
			results = results.Where(whereClause.Compile());

		return results.ToAsyncEnumerable();
	}

	///<inheritdoc/>
	public async Task<ContinuationResponse<T>> ListAsync(
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		[NotNull] ContinuationRequest request,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (orderByClause != null)
			results = orderByClause(results.AsQueryable()).AsEnumerable();

		var totalCount = request.IncludeTotalCount ? await CountAsync(null, cancellationToken) : -1L;
		var skip = 0;
		if (int.TryParse(request.ContinuationToken, out var continuationToken))
			skip = continuationToken;

		T[] resultsArray = [.. results.Skip(skip).Take(request.MaxRecords)];
		return new ContinuationResponse<T>()
		{
			Results = resultsArray,
			TotalCount = totalCount,
			RequestedCount = request.MaxRecords,
			ContinuationToken = (skip + resultsArray.Length).ToString(CultureInfo.InvariantCulture),
		};
	}

	///<inheritdoc/>
	public async Task<ContinuationResponse<T>> QueryAsync(
		Expression<Func<T, bool>> whereClause,
		Func<IQueryable<T>, IQueryable<T>>? orderByClause,
		[NotNull] ContinuationRequest request,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (orderByClause != null)
			results = orderByClause(results.AsQueryable()).AsEnumerable();
		if (whereClause != null)
			results = results.Where(whereClause.Compile());

		var totalCount = request.IncludeTotalCount ? await CountAsync(null, cancellationToken) : -1L;
		var skip = 0;
		if (int.TryParse(request.ContinuationToken, out var continuationToken))
			skip = continuationToken;

		T[] resultsArray = [.. results.Skip(skip).Take(request.MaxRecords)];
		return new ContinuationResponse<T>()
		{
			Results = resultsArray,
			TotalCount = totalCount,
			RequestedCount = request.MaxRecords,
			ContinuationToken = (skip + resultsArray.Length).ToString(CultureInfo.InvariantCulture),
		};
	}

	///<inheritdoc/>
	public Task<T?> SingleOrDefaultAsync(
		Expression<Func<T, bool>> whereClause,
		CancellationToken cancellationToken = default
	)
	{
		var results = Aggregates;
		if (whereClause != null)
			results = results.Where(whereClause.Compile());

		return Task.FromResult(results.SingleOrDefault());
	}
}
