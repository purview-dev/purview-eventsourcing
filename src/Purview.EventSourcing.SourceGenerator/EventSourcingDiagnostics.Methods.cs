using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;
static partial class EventSourcingDiagnostics
{
	public static class Methods
	{
		public static readonly EventSourcingDiagnosticDescriptor MethodIsPublic = new(
			Id: "ESSG4000",
			Title: "Apply method is public",
			Description: "The apply method should not be public.",
			Severity: DiagnosticSeverity.Warning
		);

		public static readonly EventSourcingDiagnosticDescriptor MethodHasReturnValue = new(
			Id: "ESSG4001",
			Title: "Apply method has return value",
			Description: "The apply method should not have a return value.",
			Severity: DiagnosticSeverity.Error
		);
	}
}
