namespace Purview.EventSourcing.Postgres.Events;

/// <summary>
/// Creates PostgreSQL transaction coordinators that support enlisting additional SQL work
/// alongside event-store aggregate saves.
/// </summary>
public interface IPostgresEventStoreTransactionFactory
{
	/// <summary>
	/// Creates a new PostgreSQL transaction.
	/// </summary>
	/// <param name="correlationId">
	/// Optional correlation ID shared by all enlisted aggregate saves.
	/// When <see langword="null"/>, the ambient correlation ID provider is consulted before generating a new correlation ID.
	/// </param>
	IPostgresEventStoreTransaction CreatePostgresTransaction(string? correlationId = null);
}

/// <summary>
/// Creates PostgreSQL transaction coordinators that also support enlisting additional SQL/EF Core work.
/// </summary>
/// <param name="correlationIdProvider">Provides a default correlation id when none is supplied.</param>
public sealed class PostgresEventStoreTransactionFactory(IEventStoreCorrelationIdProvider correlationIdProvider)
	: IEventStoreTransactionFactory,
		IPostgresEventStoreTransactionFactory
{
	///<inheritdoc/>
	public IEventStoreTransaction Create(string? correlationId = null) => CreatePostgresTransaction(correlationId);

	///<inheritdoc/>
	public IEventStoreTransaction Create(EventStoreTransactionOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		return CreatePostgresTransaction(options.CorrelationId);
	}

	///<inheritdoc/>
	public IPostgresEventStoreTransaction CreatePostgresTransaction(string? correlationId = null) =>
		new PostgresEventStoreTransaction(correlationId ?? correlationIdProvider.GetCorrelationId());
}
