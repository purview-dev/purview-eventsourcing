using MongoDB.Bson.Serialization.Attributes;

namespace Purview.EventSourcing.MongoDb.Entities;

public sealed class StreamVersionEntity : IEntity
{
	[BsonId]
	[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
	public string Id { get; set; } = default!;

	public int EntityType { get; set; } = EntityTypes.StreamVersionType;

	public bool IsDeleted { get; set; }

	public string AggregateType { get; set; } = default!;

	/// <summary>
	/// This is the most recently saved version of the aggregate.
	/// </summary>
	public int Version { get; set; }

	public DateTimeOffset? Timestamp { get; set; }
}
