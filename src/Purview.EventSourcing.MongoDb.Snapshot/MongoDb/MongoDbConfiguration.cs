namespace Purview.EventSourcing.MongoDb;

sealed class MongoDbConfiguration
{
	public string ConnectionString { get; set; } = default!;

	public string? ApplicationName { get; set; }

	public string Database { get; set; } = default!;

	public string Collection { get; set; } = default!;
}
