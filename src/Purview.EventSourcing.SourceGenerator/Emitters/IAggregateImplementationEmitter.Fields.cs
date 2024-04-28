using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class IAggregateImplementationEmitter
{
	static int EmitFields(IAggregateGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append(indent + 1, Constants.Shared.ConcurrentQueue, withNewLine: false)
			.Append('<')
			.Append(Constants.EventStore.IEvent)
			.Append("> _events = new ")
			.Append(Constants.Shared.ConcurrentQueue)
			.Append('<')
			.Append(Constants.EventStore.IEvent)
			.AppendLine(">();");
		;

		// Generate the event types.
		builder
			.Append(indent + 1, "readonly ", withNewLine: false)
			.Append(Constants.Shared.List)
			.Append('<')
			.Append(Constants.Shared.Type)
			.Append("> _eventTypes = new ")
			.Append(Constants.Shared.List)
			.Append('<')
			.Append(Constants.Shared.Type)
			.AppendLine(">() {")
			.Append(3, "// System events")
			.Append(3, "typeof(Purview.EventSourcing.Aggregates.Events.DeleteEvent),")
			.Append(3, "typeof(Purview.EventSourcing.Aggregates.Events.RestoreEvent),")
			.Append(3, "typeof(Purview.EventSourcing.Aggregates.Events.ForceSaveEvent),")
		;

		if (target.GeneratedApplyMethods.Length > 0)
		{

			builder.Append(indent + 2, "// Generated events");
			foreach (var generatedEvent in target.GeneratedApplyMethods)
			{
				builder
					.Append(indent + 2, "typeof(", withNewLine: false)
					.Append(generatedEvent.EventType)
					.AppendLine("),")
				;
			}
		}

		if (target.PredefinedApplyMethods.Length > 0)
		{

			builder.Append(indent + 2, "// Found pre-defined events");
			foreach (var foundEvent in target.PredefinedApplyMethods)
			{
				builder
					.Append(indent + 2, "typeof(", withNewLine: false)
					.Append(foundEvent.EventType)
					.AppendLine("),")
				;
			}
		}

		builder
			.Append(indent + 1, "};")
			.AppendLine()
		;

		return indent;
	}
}
