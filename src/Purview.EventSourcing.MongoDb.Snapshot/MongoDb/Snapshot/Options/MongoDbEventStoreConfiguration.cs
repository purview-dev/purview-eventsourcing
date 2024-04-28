using Microsoft.Extensions.Configuration;
using Purview.Options.Storage;

namespace Purview.EventSourcing.MongoDb.Snapshot.Options;

[ConfigGroup("EventStore")]
[ConfigName("MongoDbSnapshot", "Snapshot", "MongoDb")]
public class MongoDbEventStoreConfiguration : MongoDbConfiguration
{
}
