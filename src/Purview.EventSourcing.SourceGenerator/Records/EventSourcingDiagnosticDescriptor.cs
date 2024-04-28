using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator.Records;

sealed record EventSourcingDiagnosticDescriptor(
	string Id,
	string Title,
	string Description,
	DiagnosticSeverity Severity,
	string Category = Constants.Diagnostics.Activity.Usage,
	bool EnabledByDefault = true
);
