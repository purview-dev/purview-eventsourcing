using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing;

[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.DebuggerStepThrough]
public static class IEventStoreExtensions
{
	#region QuickCreate/ QuickCreateAsync

	/// <summary>
	/// Creates a new <typeparamref name="T"/>, but will not call <see cref="IAggregateIdFactory.CreateAsync{T}(CancellationToken)"/>
	/// to create a new Id. It will take the <paramref name="id"/> parameter, or the id parameter is null or empty
	/// use a new lowered <see cref="Guid"/>.
	/// </summary>
	/// <param name="eventStore">The <see cref="IEventStore{T}"/> used as the root object.</param>
	/// <typeparam name="T">The <see cref="IAggregate"/> to create.</typeparam>
	/// <param name="id">The id to use, or null with either the specified or a generated id.</param>
	/// <returns>A new aggregate of <typeparamref name="T"/>.</returns>
	/// <remarks>Calls <see cref="IEventStore{T}.FulfilRequirements(T)"/> to apply any requirements.</remarks>
	public static T QuickCreate<T>([NotNull] this IEventStore<T> eventStore, string? id = null)
		where T : class, IAggregate, new()
	{
		if (string.IsNullOrWhiteSpace(id))
			id = $"{Guid.NewGuid()}".ToLowerSafe();

		var aggregate = new T
		{
			Details = new()
			{
				Id = id
			}
		};

		eventStore.FulfilRequirements(aggregate);

		return aggregate;
	}

	public static T QuickCreate<T>(this IEventStore<T> eventStore, object? id)
		where T : class, IAggregate, new()
		=> eventStore.QuickCreate(id?.ToString());

	public static T QuickCreate<T>(this IEventStore<T> eventStore, string? id, [NotNull] Action<T> creator)
		where T : class, IAggregate, new()
	{
		var aggregate = eventStore.QuickCreate(id);

		creator(aggregate);

		return aggregate;
	}

	public static T QuickCreate<T>(this IEventStore<T> eventStore, object? id, Action<T> creator)
		where T : class, IAggregate, new()
		=> eventStore.QuickCreate(id?.ToString(), creator);

	public static async Task<T> QuickCreateAsync<T>(this IEventStore<T> eventStore, string? id, [NotNull] Func<T, CancellationToken, Task> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = eventStore.QuickCreate(id);

		await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T> QuickCreateAsync<T>(this IEventStore<T> eventStore, object? id, [NotNull] Func<T, CancellationToken, Task> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = eventStore.QuickCreate(id?.ToString());

		await creator(aggregate, cancellationToken);

		return aggregate;
	}

	#endregion QuickCreate/ QuickCreateAsync

	#region GetOrCreateAsync

	#region id: string, with context.

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id, [NotNull] Func<T, CancellationToken, Task> creator, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id, context, cancellationToken);
		if (aggregate?.IsNew() == true)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id, [NotNull] Action<T> creator, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id, context, cancellationToken);
		if (aggregate != null)
			creator(aggregate);

		return aggregate;
	}

	#endregion id: string, with context.

	#region id: string, without context.

	public static Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetOrCreateAsync(id, null, cancellationToken);

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id, [NotNull] Func<T, CancellationToken, Task> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id, null, cancellationToken);
		if (aggregate?.IsNew() == true)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id, [NotNull] Action<T> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id, null, cancellationToken);
		if (aggregate != null)
			creator(aggregate);

		return aggregate;
	}

	#endregion id: string, without context.

	#region id: object, with context.

	public static Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetOrCreateAsync(id?.ToString(), operationContext, cancellationToken);

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, [NotNull] Func<T, CancellationToken, Task> creator, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id?.ToString(), context, cancellationToken);
		if (aggregate?.IsNew() == true)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, [NotNull] Action<T> creator, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id?.ToString(), context, cancellationToken);
		if (aggregate != null)
			creator(aggregate);

		return aggregate;
	}

	#endregion id: object, with context.

	#region id: object, no context.

	public static Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetOrCreateAsync(id?.ToString(), null, cancellationToken);

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, [NotNull] Func<T, CancellationToken, Task> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id?.ToString(), null, cancellationToken);
		if (aggregate?.IsNew() == true)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T?> GetOrCreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id, [NotNull] Action<T> creator, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.GetOrCreateAsync(id?.ToString(), null, cancellationToken);
		if (aggregate != null)
			creator(aggregate);

		return aggregate;
	}

	#endregion id: object, no context.

	#endregion GetOrCreateAsync

	#region CreateAsync

	public static async Task<T> CreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id = null, Func<T, CancellationToken, Task>? creator = null, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.CreateAsync(id, cancellationToken);
		if (creator != null)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T> CreateAsync<T>([NotNull] this IEventStore<T> eventStore, string? id = null, Action<T>? creator = null, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.CreateAsync(id, cancellationToken);
		creator?.Invoke(aggregate);

		return aggregate;
	}

	public static Task<T> CreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id = null, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.CreateAsync(id?.ToString(), cancellationToken);

	public static async Task<T> CreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id = null, Func<T, CancellationToken, Task>? creator = null, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.CreateAsync(id?.ToString(), cancellationToken);
		if (creator != null)
			await creator(aggregate, cancellationToken);

		return aggregate;
	}

	public static async Task<T> CreateAsync<T>([NotNull] this IEventStore<T> eventStore, object? id = null, Action<T>? creator = null, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var aggregate = await eventStore.CreateAsync(id?.ToString(), cancellationToken);
		creator?.Invoke(aggregate);

		return aggregate;
	}

	#endregion CreateAsync

	#region GetAsync

	public static Task<T?> GetAsync<T>([NotNull] this IEventStore<T> eventStore, string id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetAsync(id, null, cancellationToken);

	public static Task<T?> GetAsync<T>([NotNull] this IEventStore<T> eventStore, object id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();

		return idAsString is null ? throw NullAggregateId(id) : eventStore.GetAsync(idAsString, null, cancellationToken);
	}

	public static Task<T?> GetAsync<T>([NotNull] this IEventStore<T> eventStore, object id, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();

		return idAsString is null
			? throw NullAggregateId(id)
			: eventStore.GetAsync(idAsString, context, cancellationToken);
	}

	#endregion GetAsync

	#region GetAtAsync

	public static Task<T?> GetAtAsync<T>([NotNull] this IEventStore<T> eventStore, string aggregateId, int version, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetAtAsync(aggregateId, version, null, cancellationToken);

	public static Task<T?> GetAtAsync<T>([NotNull] this IEventStore<T> eventStore, object aggregateId, int version, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.GetAtAsync(aggregateId, version, null, cancellationToken);

	public static Task<T?> GetAtAsync<T>([NotNull] this IEventStore<T> eventStore, object aggregateId, int version, EventStoreOperationContext? context, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = aggregateId?.ToString();
		return idAsString is null
			? throw NullAggregateId(aggregateId)
			: eventStore.GetAtAsync(idAsString, version, context, cancellationToken);
	}

	#endregion GetAtAsync

	#region IsDeletedAsync

	public static Task<bool> IsDeletedAsync<T>([NotNull] this IEventStore<T> eventStore, object id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();
		return idAsString is null
			? throw NullAggregateId(id)
			: eventStore.IsDeletedAsync(idAsString, cancellationToken);
	}

	#endregion IsDeletedAsync

	#region GetDeletedAsync

	public static Task<T?> GetDeletedAsync<T>([NotNull] this IEventStore<T> eventStore, object id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();
		return idAsString is null
			? throw NullAggregateId(id)
			: eventStore.GetDeletedAsync(idAsString, cancellationToken);
	}

	#endregion GetDeletedAsync

	#region ExistsAsync

	public static Task<ExistsState> ExistsAsync<T>([NotNull] this IEventStore<T> eventStore, object id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();
		return idAsString is null
			? throw NullAggregateId(id)
			: eventStore.ExistsAsync(idAsString, cancellationToken);
	}

	public static Task<ExistsState> ExistsWithNullCheckAsync<T>([NotNull] this IEventStore<T> eventStore, object id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		var idAsString = id?.ToString();
		return idAsString is null
			? throw NullAggregateId(id)
			: eventStore.ExistsWithNullCheckAsync(idAsString, cancellationToken);
	}

	public static Task<ExistsState> ExistsWithNullCheckAsync<T>([NotNull] this IEventStore<T> eventStore, string id, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		return string.IsNullOrWhiteSpace(id)
			? Task.FromResult(ExistsState.DoesNotExists)
			: eventStore.ExistsAsync(id, cancellationToken);
	}

	#endregion ExistsAsync

	#region SaveAsync

	public static Task<SaveResult<T>> SaveAsync<T>([NotNull] this IEventStore<T> eventStore, T aggregate, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.SaveAsync(aggregate, null, cancellationToken);

	#endregion SaveAsync

	#region DeleteAsync

	public static Task<bool> DeleteAsync<T>([NotNull] this IEventStore<T> eventStore, T aggregate, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.DeleteAsync(aggregate, null, cancellationToken);

	public static Task<bool> DeleteAsync<T>([NotNull] this IEventStore<T> eventStore, string aggregateId, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.DeleteAsync(aggregateId, null, cancellationToken);

	public static async Task<bool> DeleteAsync<T>([NotNull] this IEventStore<T> eventStore, string aggregateId, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId, nameof(aggregateId));

		var aggregate = await eventStore.GetAsync(aggregateId, operationContext, cancellationToken);
		return aggregate != null && await eventStore.DeleteAsync(aggregate, operationContext, cancellationToken);
	}

	#endregion DeleteAsync

	#region RestoreAsync

	public static Task<bool> RestoreAsync<T>([NotNull] this IEventStore<T> eventStore, T aggregate, CancellationToken cancellationToken = default)
		where T : class, IAggregate, new()
		=> eventStore.RestoreAsync(aggregate, null, cancellationToken);

	#endregion RestoreAsync

	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "It is used.")]
	static ArgumentNullException NullAggregateId(object? id)
		=> new(nameof(id), "When converted to a string, the id is either null or empty.");
}
