using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace Purview.EventSourcing.CosmosDb;

public class CosmosDbIndexOptions
{
	public bool Automatic { get; set; } = true;

	[EnumDataType(typeof(IndexingMode))]
	public IndexingMode IndexingModel { get; set; } = IndexingMode.Consistent;

	public string[] IncludedPaths { get; set; } = [];

	public string[] ExcludedPaths { get; set; } = [];

	public SpatialPath[] SpatialIndices { get; set; } = [];

	public CompositePath[][] CompositeIndices { get; set; } = [];

	public override int GetHashCode()
	{
		var hashCode = new HashCode();

		hashCode.Add(IndexingModel);
		if (IncludedPaths != null)
			hashCode.Add(IncludedPaths);

		if (ExcludedPaths != null)
			hashCode.Add(ExcludedPaths);

		if (SpatialIndices != null)
			hashCode.Add(SpatialIndices);

		if (CompositeIndices != null)
			hashCode.Add(CompositeIndices);

		return hashCode.ToHashCode();
	}
}
