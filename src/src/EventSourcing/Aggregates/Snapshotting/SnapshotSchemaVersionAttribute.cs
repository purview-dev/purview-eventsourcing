namespace Purview.EventSourcing.Aggregates.Snapshotting;

/// <summary>Declares the serialization schema version of an aggregate snapshot.</summary>
/// <remarks>
/// Increment the version when an existing snapshot can no longer be safely deserialized. The event stream is then
/// replayed to rebuild canonical state, and the next snapshot-eligible save replaces the incompatible snapshot.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SnapshotSchemaVersionAttribute : Attribute
{
	/// <summary>Creates an attribute with a positive snapshot schema version.</summary>
	public SnapshotSchemaVersionAttribute(int version)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
		Version = version;
	}

	/// <summary>Gets the declared snapshot schema version.</summary>
	public int Version { get; }
}
