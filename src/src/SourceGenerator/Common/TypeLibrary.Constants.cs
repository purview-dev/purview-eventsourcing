namespace Purview.EventSourcing.SourceGenerator.Common;

public static partial class TypeLibrary
{
	public const string AggregateNamespace = "Purview.EventSourcing.Aggregates";

	public const string EventsNamespace = "Purview.EventSourcing.Aggregates.Events";

	public const string SerializationNamespace = "Purview.EventSourcing.Serialization";

	public const string CollectionsNamespace = "Purview.EventSourcing";

	public const string AggregateGeneratorName = "Purview.EventSourcing.AggregateSourceGenerator";

	public const string ValueObjectGeneratorName = "Purview.EventSourcing.ValueObjectSourceGenerator";

	public const string ValueObjectAttributeFullTypeName = SerializationNamespace + ".ValueObjectAttribute";

	public const string ValueObjectDefaultsAttributeFullTypeName =
		SerializationNamespace + ".ValueObjectDefaultsAttribute";

	public const string ScalarAttributeFullTypeName = SerializationNamespace + ".ScalarAttribute";

	public const string ValueObjectDeserializationModeFullTypeName =
		SerializationNamespace + ".ValueObjectDeserializationMode";

	public const string AggregateAttributeFullTypeName = AggregateNamespace + ".AggregateAttribute";

	public const string AggregateDefaultsAttributeFullTypeName = AggregateNamespace + ".AggregateDefaultsAttribute";

	public const string CollectionEventAttributeFullTypeName = AggregateNamespace + ".CollectionEventAttribute";

	public const string EventAttributeFullTypeName = AggregateNamespace + ".EventAttribute";

	public const string MetadataAttributeFullTypeName = AggregateNamespace + ".MetadataAttribute";

	public const string PropertyAttributeFullTypeName = AggregateNamespace + ".PropertyAttribute";
}
