using System.Reflection;

namespace Purview.EventSourcing.Aggregates.Snapshotting;

/// <summary>Resolves aggregate snapshot schema versions.</summary>
public static class AggregateSnapshotSchema
{
	/// <summary>Gets the declared schema version, or 1 when no declaration is present.</summary>
	public static int GetVersion<T>() => GetVersion(typeof(T));

	/// <summary>Gets the declared schema version, or 1 when no declaration is present.</summary>
	public static int GetVersion(Type aggregateType)
	{
		ArgumentNullException.ThrowIfNull(aggregateType);
		return aggregateType.GetCustomAttribute<SnapshotSchemaVersionAttribute>(inherit: true)?.Version ?? 1;
	}

	/// <summary>Returns a stable storage suffix for versions newer than the legacy version 1.</summary>
	public static string GetStorageSuffix<T>()
	{
		var version = GetVersion<T>();
		return version == 1 ? string.Empty : $":sv{version}";
	}
}
