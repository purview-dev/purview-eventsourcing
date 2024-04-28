using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;

static partial class EventSourcingDiagnostics
{
	public const string IdentityNameKey = "{IdentityName}";
	public const string KindKey = "{Kind}";

	public static void Report(this Action<Diagnostic> report, EventSourcingDiagnosticDescriptor eventSourceDiagnostic, Location? location, params object?[] args)
	{
		var diagnostic = Diagnostic.Create(
			new(
				id: eventSourceDiagnostic.Id,
				title: eventSourceDiagnostic.Title,
				messageFormat: eventSourceDiagnostic.Description,
				category: eventSourceDiagnostic.Category,
				defaultSeverity: eventSourceDiagnostic.Severity,
				isEnabledByDefault: eventSourceDiagnostic.EnabledByDefault
			),
			location,
			args
		);

		report(diagnostic);
	}
}
