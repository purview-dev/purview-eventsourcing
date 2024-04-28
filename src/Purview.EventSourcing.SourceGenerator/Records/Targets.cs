using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Records;

/// <summary>
/// This represents a class that is explicitly requesting
/// IAggregate generation.
/// </summary>
/// <param name="ClassName">The name of the class.</param>
/// <param name="ClassNamespace">The namespace of the class.</param>
/// <param name="ParentClasses">Any parent classes, in the case where the target is nested.</param>
/// <param name="FullNamespace">The full namespace, including any defined namespace and parent classes.</param>
/// <param name="FullyQualifiedName">The fully qualified name of the class - including namespace and parent classes.</param>
/// <param name="PredefinedApplyMethods">All methods that match 'METHODNAME(EVENTTYPE)'.</param>
/// <param name="GeneratedApplyMethods">All methods that will exist when the EventProperty is generated.</param>
/// <param name="IsPartialClass">If false, generates a diagnostic error</param>
/// <param name="IAggregateInterfaceIsPresent">If false, generates a diagnostic error.</param>
/// <param name="GenerateIAggregateImplicitly">If true, generates the IAggregate implicitly.</param>
/// <param name="GenerateAggregateTypeProperty">If true, indicates that the AggregateType needs generating. Otherwise, it's been implemented elsewhere.</param>
/// <param name="AggregateTypeName">The name of the aggregate type to generate, or null if the property already exists.</param>
/// <param name="ClassLocation">The location of the class used as a source for generation.</param>
record IAggregateGenerationTarget(
	string ClassName,
	string? ClassNamespace, string[] ParentClasses,
	string? FullNamespace, string FullyQualifiedName,
	ImmutableArray<FoundPreDefinedApplyMethod> PredefinedApplyMethods,
	ImmutableArray<EventPropertyApplyMethodDetails> GeneratedApplyMethods,
	bool IsPartialClass, bool IAggregateInterfaceIsPresent,
	bool GenerateIAggregateImplicitly,
	bool GenerateAggregateTypeProperty, string? AggregateTypeName,
	Location? ClassLocation
	)
{
}

/// <summary>
/// This represents a method that will be generated when the EventProperty is generated.
/// </summary>
/// <param name="MethodName">The name of the apply method to generate.</param>
/// <param name="EventType">The event type, sans any namespace information.</param>
/// <param name="PreDefinedMethod">If this method was based on a pre-existing one.</param>
record EventPropertyApplyMethodDetails(
	string MethodName,
	string EventType,
	FoundPreDefinedApplyMethod? PreDefinedMethod
)
{
	/// <summary>
	/// If true, the method will be generated otherwise it's already been defined.
	/// </summary>
	public bool GenerateMethod => PreDefinedMethod == null;
}

/// <summary>
/// This represents a field requesting property, applier and event generation.
/// </summary>
/// <param name="ParentClassHasGenerateIAggregate">Indicates if the parent class has the GenerateIAggregateAttribute method.</param>
/// <param name="AggregateClassName">The name of the owning aggregate class.</param>
/// <param name="AggregateClassNamespace">The namespace of the owning aggregate class.</param>
/// <param name="AggregateParentClasses">Any parent classes, in the case where the target is nested.</param>
/// <param name="FullyQualifiedName">The fully qualified name of the Event class - including namespace and parent classes.</param>
/// <param name="EventTypeName">The name of the event type to generate.</param>
/// <param name="FieldName">The name of the field where the attribute is applied.
/// <param name="PropertyName">The name of the property to generate.
/// <param name="PropertyType">The fully-qualified type of the property to generate.
/// <param name="IsNullable">If true, the property type is nullable.</param>
/// <param name="PrivateSetter">If true, generates a private setter.</param>
/// <param name="GenerateApplyMethod">If true, generates an apply method.</param>"
/// <param name="ApplyMethodName">The name of the apply method to generate.</param>
/// <param name="FieldLocation">The location of the field used as a source for generation.</param>
record EventPropertyGenerationTarget(
	bool ParentClassHasGenerateAggregate,
	string AggregateClassName,
	string? AggregateClassNamespace,
	string[] AggregateParentClasses,
	string FullyQualifiedName,

	string EventTypeName,

	string FieldName,
	string PropertyName,
	string PropertyType,
	bool IsNullable,

	bool PrivateSetter,

	bool GenerateApplyMethod,
	string ApplyMethodName,

	Location? FieldLocation
);

/// <summary>
/// Represents an Apply(Event) method found on the class, entirely user-defined.
/// </summary>
/// <param name="PartialMatch">Indicates a partial match, which could be due to a generated event.</param>
/// <param name="MethodName">The name of the pre-defined Apply method located.</param>
/// <param name="EventType">The type of the event used, if the case of a partial match this will be whatever the syntax node has.</param>
/// <param name="IsVoid">If false, generate diagnostic.</param>
/// <param name="IsPublic">If true, generate diagnostic.</param>
/// <param name="MethodLocation">The location of the method used as a source for generation.</param>"
record FoundPreDefinedApplyMethod(
	bool PartialMatch,
	string MethodName,
	string EventType,
	bool IsVoid,
	bool IsPublic,
	Location? MethodLocation
);
