namespace Purview.EventSourcing.Aggregates.Snapshotting;

public sealed class AggregateSnapshotSchemaTests
{
	[Test]
	public async Task GetVersion_GivenNoAttribute_ReturnsLegacyVersion()
	{
		await Assert.That(AggregateSnapshotSchema.GetVersion<LegacyAggregate>()).IsEqualTo(1);
		await Assert.That(AggregateSnapshotSchema.GetStorageSuffix<LegacyAggregate>()).IsEmpty();
	}

	[Test]
	public async Task GetVersion_GivenVersionedBaseType_InheritsVersion()
	{
		await Assert.That(AggregateSnapshotSchema.GetVersion<VersionedAggregate>()).IsEqualTo(3);
		await Assert.That(AggregateSnapshotSchema.GetStorageSuffix<VersionedAggregate>()).IsEqualTo(":sv3");
	}

	[Test]
	public async Task Attribute_GivenNonPositiveVersion_Throws()
	{
		await Assert.That(() => new SnapshotSchemaVersionAttribute(0)).Throws<ArgumentOutOfRangeException>();
	}

	sealed class LegacyAggregate;

	[SnapshotSchemaVersion(3)]
	class VersionedAggregateBase;

	sealed class VersionedAggregate : VersionedAggregateBase;
}
