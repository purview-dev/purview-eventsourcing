namespace Purview.EventSourcing.SqlServer.Events;

/// <summary>
/// Creates SQL Server transaction coordinators that support enlisting additional SQL work
/// alongside event-store aggregate saves.
/// </summary>
public interface ISqlServerEventStoreTransactionFactory
{
	/// <summary>
	/// Creates a new SQL Server transaction.
	/// </summary>
	/// <param name="correlationId">
	/// Optional correlation ID shared by all enlisted aggregate saves.
	/// When <see langword="null"/>, the ambient correlation ID provider is consulted before generating a new correlation ID.
	/// </param>
	ISqlServerEventStoreTransaction CreateSqlServerTransaction(string? correlationId = null);
}

/// <summary>
/// Default implementation of <see cref="ISqlServerEventStoreTransactionFactory"/> that consults the ambient
/// correlation-ID provider when no explicit correlation ID is supplied.
/// </summary>
/// <remarks>
/// Creates <see cref="SqlServerEventStoreTransaction"/> instances that support enlisting additional SQL/EF work
/// alongside event-store aggregate saves.
/// </remarks>
/// <param name="correlationIdProvider">The provider consulted to generate a correlation ID when none is supplied.</param>
public sealed class SqlServerEventStoreTransactionFactory(IEventStoreCorrelationIdProvider correlationIdProvider)
	: IEventStoreTransactionFactory,
		ISqlServerEventStoreTransactionFactory
{
	///<inheritdoc/>
	public IEventStoreTransaction Create(string? correlationId = null) => CreateSqlServerTransaction(correlationId);

	///<inheritdoc/>
	public IEventStoreTransaction Create(EventStoreTransactionOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		return CreateSqlServerTransaction(options.CorrelationId);
	}

	///<inheritdoc/>
	public ISqlServerEventStoreTransaction CreateSqlServerTransaction(string? correlationId = null) =>
		new SqlServerEventStoreTransaction(correlationId ?? correlationIdProvider.GetCorrelationId());
}
