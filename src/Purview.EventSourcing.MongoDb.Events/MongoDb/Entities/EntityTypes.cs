namespace Purview.EventSourcing.MongoDb.Entities;

static class EntityTypes
{
	public const int StreamVersionType = 0;
	public const int EventType = 1;
	public const int IdempotencyMarkerType = 2;
}
