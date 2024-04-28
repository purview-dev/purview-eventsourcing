using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingDiagnostics
{
	public static class General
	{
		public static readonly EventSourcingDiagnosticDescriptor FatalExecutionDuringExecution = new(
			Id: "ESSG1000",
			Title: "Fatal execution error occurred",
			Description: "Failed to execute the generation stage: {0}",
			Severity: DiagnosticSeverity.Error
		);
	}
}
