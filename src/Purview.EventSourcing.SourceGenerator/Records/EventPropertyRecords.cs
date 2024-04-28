namespace Purview.EventSourcing.SourceGenerator.Records;

record EventPropertyAttributeRecord(
	AttributeStringValue PropertyName,
	AttributeValue<bool> PrivateSetter,
	AttributeStringValue EventTypeName
);
