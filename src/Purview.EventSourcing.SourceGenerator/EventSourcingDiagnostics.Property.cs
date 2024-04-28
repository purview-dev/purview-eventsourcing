using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingDiagnostics
{
	public static class Property
	{
		public static readonly EventSourcingDiagnosticDescriptor ParentDoesNotHaveGenerateAggregateAttribute = new(
			Id: "ESSG3000",
			Title: "Missing GenerateAggregateAttribute",
			Description: "Parent class does not contain the GenerateAggregateAttribute.",
			Severity: DiagnosticSeverity.Error
		);
	}
}
