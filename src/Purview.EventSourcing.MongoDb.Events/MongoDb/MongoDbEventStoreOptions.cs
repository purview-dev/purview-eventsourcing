using System.ComponentModel.DataAnnotations;

namespace Purview.EventSourcing.MongoDb;

sealed public class MongoDbEventStoreOptions
{
	public const string MongoDbEventStore = "EventStore:MongoDb";

	[Required]
	public string ConnectionString { get; set; } = default!;

	public string? ApplicationName { get; set; }

	[Required]
	public string Database { get; set; } = default!;

	public string? Collection { get; set; }
}
