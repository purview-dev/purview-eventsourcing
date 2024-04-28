using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;
partial class EventSourcingDiagnostics
{
	public static class TopLevelClass
	{
		public static readonly EventSourcingDiagnosticDescriptor DoesNotImplementIAggregate = new(
			Id: "ESSG2000",
			Title: "Class does not implement IAggregate",
			Description: "{0} must implement {1}.",
			Severity: DiagnosticSeverity.Error
		);

		public static readonly EventSourcingDiagnosticDescriptor ClassIsNotPartial = new(
			Id: "ESSG2001",
			Title: "Class is not a partial class",
			Description: "{0} should be partial.",
			Severity: DiagnosticSeverity.Error
		);
	}
}
