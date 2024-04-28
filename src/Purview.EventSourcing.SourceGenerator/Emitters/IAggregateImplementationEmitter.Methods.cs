using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class IAggregateImplementationEmitter
{
	static int EmitMethods(IAggregateGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, Constants.Shared.Boolean), withNewLine: false)
			.Append(' ')
			.Append(PublicOrImplicitIAggregate(target))
			.AppendLine("HasUnsavedEvents() { return !_events.IsEmpty; }")
			.AppendLine()
		;

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, Constants.Shared.IEnumerable), withNewLine: false)
			.Append('<')
			.Append(Constants.Shared.Type)
			.Append("> ")
			.Append(PublicOrImplicitIAggregate(target))
			.AppendLine("GetRegisteredEventTypes() { return _eventTypes.ToArray(); }")
			.AppendLine()
		;

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, Constants.Shared.IEnumerable), withNewLine: false)
			.Append('<')
			.Append(Constants.EventStore.IEvent.FullName)
			.Append("> ")
			.Append(PublicOrImplicitIAggregate(target))
			.AppendLine("GetUnsavedEvents() { return _events.ToArray(); }")
			.AppendLine()
		;

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, Constants.Shared.Boolean), withNewLine: false)
			.Append(' ')
			.Append(PublicOrImplicitIAggregate(target))
			.Append("CanApplyEvent(")
			.Append(Constants.EventStore.IEvent)
			.AppendLine(" aggregateEvent) { return _eventTypes.Contains(aggregateEvent.GetType()); }")
			.AppendLine()
		;

		var implicitAccessor = target.GenerateIAggregateImplicitly
			? "((Purview.EventSourcing.Interfaces.Aggregates.IAggregate)this)."
			: "";

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, "void"), withNewLine: false)
			.Append(' ')
			.Append(PublicOrImplicitIAggregate(target))
			.Append("ClearUnsavedEvents(int? upToVersion)") // the default `= null` is implicit here.
			.Append(@$"
		{{
			var unsavedEventCount = _events.Count;
			if (upToVersion.HasValue)
			{{
				_events = new System.Collections.Concurrent.ConcurrentQueue<Purview.EventSourcing.Interfaces.Aggregates.Events.IEvent>(
					System.Linq.Enumerable.Where(_events, m => m.Details.AggregateVersion > upToVersion)
				);
			}}
			else
			{{
				_events.Clear();
			}}

			unsavedEventCount -= _events.Count;

			var details = {implicitAccessor}Details;

			details.CurrentVersion -= unsavedEventCount;
		}}

		void RecordAndApplyEvent(Purview.EventSourcing.Interfaces.Aggregates.Events.IEvent @event)
		{{
			RecordEvent(@event);
			{implicitAccessor}ApplyEvent(@event);
		}}

");

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, "void"), withNewLine: false)
			.Append(' ')
			.Append(PublicOrImplicitIAggregate(target))
			.Append(@$"ApplyEvent(Purview.EventSourcing.Interfaces.Aggregates.Events.IEvent @event)
		{{
			var details = {implicitAccessor}Details;
			if (details.Locked)
			{{
				throw new Purview.EventSourcing.Aggregates.Exceptions.LockedException(details.Id);
			}}

			if (@event.Details.AggregateVersion == 1)
			{{
				details.Created = @event.Details.When;
			}}

			details.Updated = @event.Details.When;
			details.CurrentVersion = @event.Details.AggregateVersion;

			if (@event is Purview.EventSourcing.Aggregates.Events.DeleteEvent)
			{{
				ApplySystemEvent((Purview.EventSourcing.Aggregates.Events.DeleteEvent)@event);
			}}
			else if (@event is Purview.EventSourcing.Aggregates.Events.RestoreEvent)
			{{
				ApplySystemEvent((Purview.EventSourcing.Aggregates.Events.RestoreEvent)@event);
			}}
			else if (@event is Purview.EventSourcing.Aggregates.Events.ForceSaveEvent)
			{{
				ApplySystemEvent((Purview.EventSourcing.Aggregates.Events.ForceSaveEvent)@event);
			}}
")
		;

		// Finishing the apply methods by methods that were not generated.
		foreach (var eventTarget in target.GeneratedApplyMethods)
		{
			builder
				.Append(indent + 2, @"else if (@event is ", withNewLine: false)
				.Append(eventTarget.EventType)
				.AppendLine(')')
				.Append(indent + 2, '{')
				.Append(indent + 3, eventTarget.MethodName, withNewLine: false)
				.Append("((")
				.Append(eventTarget.EventType)
				.AppendLine(")@event);")
				.Append(indent + 2, '}')
			;
		}

		// Adding any additional apply methods we found along the way
		foreach (var eventTarget in target.PredefinedApplyMethods)
		{
			builder
				.Append(indent + 2, @"else if (@event is ", withNewLine: false)
				.Append(eventTarget.EventType)
				.AppendLine(')')
				.Append(indent + 2, '{')
				.Append(indent + 3, eventTarget.MethodName, withNewLine: false)
				.Append("((")
				.Append(eventTarget.EventType)
				.AppendLine(")@event);")
				.Append(indent + 2, '}')
			;
		}

		builder.Append(indent + 1, @$"}}

		void RecordEvent(Purview.EventSourcing.Interfaces.Aggregates.Events.IEvent @event)
		{{
			var details = {implicitAccessor}Details;
			if (details.Locked)
			{{
				throw new Purview.EventSourcing.Aggregates.Exceptions.LockedException(details.Id);
			}}

			@event.Details.AggregateVersion = details.CurrentVersion + 1;
			@event.Details.When = System.DateTimeOffset.UtcNow;

			_events.Enqueue(@event);
		}}

		void ApplySystemEvent(Purview.EventSourcing.Aggregates.Events.DeleteEvent @event)
		{{
			var details = {implicitAccessor}Details;
			details.IsDeleted = true;
		}}

		void ApplySystemEvent(Purview.EventSourcing.Aggregates.Events.RestoreEvent @event)
		{{
			var details = {implicitAccessor}Details;
			details.IsDeleted = false;
		}}

		static void ApplySystemEvent(Purview.EventSourcing.Aggregates.Events.ForceSaveEvent _)
		{{
			// This is intentionally left blank.
		}}")
		;

		return indent;
	}
}
