using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.Postgres.Outbox;

/// <summary>
/// Options that configure the PostgreSQL outbox store.
/// </summary>
/// <remarks>
/// Bound from the <c>EventStore:Postgres:Outbox</c> configuration section. When
/// <see cref="ConnectionString"/> is not supplied, the event-store connection string is used.
/// </remarks>
public sealed class PostgresOutboxStoreOptions
{
	/// <summary>The configuration section name used to bind these options.</summary>
	public const string PostgresOutbox = "EventStore:Postgres:Outbox";

	/// <summary>The default table name used to persist outbox messages.</summary>
	public const string DefaultTableName = "EventStoreOutbox";

	/// <summary>
	/// The connection string used to access the PostgreSQL database. When null, the event-store
	/// connection string is used.
	/// </summary>
	public string? ConnectionString { get; set; }

	/// <summary>The schema that owns the outbox table.</summary>
	[Required(AllowEmptyStrings = false)]
	[RegularExpression(@"^[\w\-.]+$")]
	public string SchemaName { get; set; } = "public";

	/// <summary>The name of the table that stores outbox messages.</summary>
	[Required(AllowEmptyStrings = false)]
	[RegularExpression(@"^[\w\-.]+$")]
	public string TableName { get; set; } = DefaultTableName;

	/// <summary>When true, the outbox table is created if it does not exist.</summary>
	public bool AutoCreateTable { get; set; } = true;
}
