using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class EventTargetClassEmitter
{
	static int EmitEventProperty(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating properties for: {target.FullyQualifiedName}");

		builder
			.Append(indent + 1, "public ", withNewLine: false)
			.Append(Constants.EventStore.EventDetails)
			.Append(" Details { get; set; } = new ")
			.Append(Constants.EventStore.EventDetails)
			.AppendLine("();")
			.AppendLine()
		;

		builder
			.Append(indent + 1, "public ", withNewLine: false)
			.Append(target.PropertyType);

		if (target.IsNullable)
		{
			builder.Append('?');
		}

		builder
			.Append(' ')
			.Append(target.PropertyName)
			.AppendLine(" { get; set; } = default!;")
		;

		return indent;
	}

	static int EmitAggregateProperty(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating aggregate properties for: {target.FullyQualifiedName}");

		builder
			.Append(indent + 1, "// Property for field: ", withNewLine: false)
			.Append(target.FieldName)
			.AppendLine();
		;

		builder
			.Append(indent + 1, "public ", withNewLine: false)
			.Append(target.PropertyType);

		if (target.IsNullable)
		{
			builder.Append('?');
		}

		builder
			.Append(' ')
			.Append(target.PropertyName)
			.AppendLine()
			.Append(indent + 1, '{')
		;

		// Getter
		builder
			.Append(indent + 2, "get => ", withNewLine: false)
			.Append(target.FieldName)
			.Append(';')
			.AppendLine()
		;

		// Setter

		// Start of setter
		builder.AppendTabs(indent + 2);

		if (target.PrivateSetter)
		{
			builder.Append("private ");
		}

		builder
			.Append("set")
			.AppendLine()
			.Append(indent + 2, '{');

		// Output:
		// if (Comparer<T>.Default.Compare(value, _field) != 0)
		builder
			.Append(indent + 3, "if (", withNewLine: false)
			.Append(Constants.Shared.Comparer)
			.Append('<')
			.Append(target.PropertyType)
			.Append('>')
			.Append(".Default.Compare(value, ")
			.Append(target.FieldName)
			.Append(") != 0)")
			.AppendLine()
		;

		// Output:
		// {
		//		RecordAndApplyEvent(new EVENTNAME_GOES_HERE() { FIELDNAME_GOES_HERE = value });
		// }
		builder
			.Append(indent + 3, '{')
			.Append(indent + 4, "RecordAndApplyEvent(new ", withNewLine: false)
			.Append(target.FullyQualifiedName)
			.Append("() { ")
			.Append(target.PropertyName)
			.Append(" = value });")
			.AppendLine()
			.Append(indent + 3, '}')
		;

		// End of setter.
		builder.Append(indent + 2, '}');

		// End of the property.
		builder
			.Append(indent + 1, '}')
		;

		return indent;
	}
}
