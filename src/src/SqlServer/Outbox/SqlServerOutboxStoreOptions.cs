using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.SqlServer.Outbox;

/// <summary>
/// Options that configure the SQL Server outbox store.
/// </summary>
/// <remarks>
/// Bound from the <c>EventStore:SqlServer:Outbox</c> configuration section. When
/// <see cref="ConnectionString"/> is not supplied, the event-store connection string is used.
/// </remarks>
public sealed class SqlServerOutboxStoreOptions
{
	/// <summary>The configuration section name used to bind these options.</summary>
	public const string SqlServerOutbox = "EventStore:SqlServer:Outbox";

	/// <summary>The default table name used to persist outbox messages.</summary>
	public const string DefaultTableName = "EventStoreOutbox";

	/// <summary>
	/// The connection string used to access the SQL Server database. When null, the event-store
	/// connection string is used.
	/// </summary>
	public string? ConnectionString { get; set; }

	/// <summary>The schema that owns the outbox table.</summary>
	[Required(AllowEmptyStrings = false)]
	[RegularExpression(@"^[\w\-.]+$")]
	public string SchemaName { get; set; } = "dbo";

	/// <summary>The name of the table that stores outbox messages.</summary>
	[Required(AllowEmptyStrings = false)]
	[RegularExpression(@"^[\w\-.]+$")]
	public string TableName { get; set; } = DefaultTableName;

	/// <summary>When true, the outbox table is created if it does not exist.</summary>
	public bool AutoCreateTable { get; set; } = true;
}
