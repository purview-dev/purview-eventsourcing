namespace Purview.EventSourcing.MongoDb.Entities;

interface IEntity
{
	string Id { get; set; }

	int EntityType { get; }
}
