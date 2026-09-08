namespace Purview.EventSourcing.SourceGenerator.Aggregate.Models;

[Generate(TypeLibrary.AggregateAttributeFullTypeName)]
readonly partial record struct AggregateAttributeData(string? EventNamespace, string? EventSuffix);

[Generate(TypeLibrary.AggregateDefaultsAttributeFullTypeName)]
readonly partial record struct AggregateDefaultsAttributeData(string? EventSuffix, TypeIdentity EventType);

[Generate(TypeLibrary.CollectionEventAttributeFullTypeName)]
readonly partial record struct CollectionEventAttributeData(
	[Argument("propertyName", defaultValue: "")] string PropertyName,
	[Property(1)] int Version,
	string? EventName,
	string? EventNamespace,
	[Property("Auto", IsEnum = true)] string Operation,
	bool Manual
);

[Generate(TypeLibrary.EventAttributeFullTypeName)]
readonly partial record struct EventAttributeData(
	[Property(1)] int Version,
	string? EventName,
	string? EventNamespace,
	bool Manual
);

[Generate(TypeLibrary.MetadataAttributeFullTypeName)]
readonly partial record struct MetadataAttributeData([Argument("store", true)] bool Store);

[Generate(TypeLibrary.PropertyAttributeFullTypeName)]
readonly partial record struct PropertyAttributeData([Argument("propertyName", defaultValue: "")] string PropertyName);
