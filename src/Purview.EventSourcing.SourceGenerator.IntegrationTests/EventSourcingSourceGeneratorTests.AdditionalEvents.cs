namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenAdditionalEvents_IncludesEventInfoInGeneration()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;
using Purview.EventSourcing.Interfaces.Aggregates.Events;
using Purview.EventSourcing.Aggregates.Events;

namespace Testing;

[GenerateAggregate]
public partial class AdditionalEventsTestAggregate : IAggregate {
	[EventProperty]
	string? _stringValue;

	void ApplyCustomIEvent(CustomIEvent @event) {
		// Do something with the event
	}

	void ApplyCustomEventBase(CustomEventBase @event) {
		// Do something with the event
	}
}

class CustomIEvent : IEvent {
	public EventDetails Details { get; } = new();
}

class CustomEventBase : EventBase {
	override protected void BuildEventHash(ref HashCode hash) {
		// Do something with the hash
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
