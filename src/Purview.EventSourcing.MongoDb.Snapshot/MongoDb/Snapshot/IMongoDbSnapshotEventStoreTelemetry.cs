using Purview.Telemetry.Metrics;

namespace Purview.EventSourcing.MongoDb.Snapshot;

[Meter]
public interface IMongoDbSnapshotEventStoreTelemetry
{
	[Counter(AutoIncrement = true)]
	void SnapshotCreated(string aggregateType);
}
