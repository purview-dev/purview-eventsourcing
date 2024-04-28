namespace Purview.EventSourcing.SourceGenerator.Resources;

public partial class X : IAggregate, IRequirement<TService> {
	TService _service = default!;

	[EventProperty]
	string? _stringValue;

	[EventProperty(false)]
	int _intValue;

	public int Result { get; private set; }

	//public X() {
	//	// Why can't we detect and automate this?
	//	// Find custom events with the apply method.
	//	_eventTypes.Add(typeof(ACustomEvent));
	//}

	public void DoACustomThing(int x, string y) {
		RecordAndApplyEvent(new CustomEvent { X = x, Y = y });
	}

	public void SetStringValue(string? value) {
		// Also set here...
		_service.ValidateValue(value);

		StringValue = value;
	}

	void IRequirement<TService>.Requires(TService service) {
		_service = service;
	}

	void ApplyACustomEvent(ACustomEvent @event) {
		Result = @event.X + @event.Y;
	}
}

// SHARED GENERATED CODE

partial class X {
	readonly ConcurrentQueue<IEvent> _events = [];
	readonly List<Type> _eventTypes = [
		// System events
		typeof(),
		typeof(RestoreEvent),
		typeof(ForceSaveEvent),

		// Source Generated events
		typeof(StringValueEvent),
		typeof(IntValueEvent),

		// Found events
		typeof(ACustomEvent)
	];

	// IF it doesn't exist...?
	string IAggregate.AggregateType { get; } = "generated-name";

	IEnumerable<Type> IAggregate.GetRegisteredEventTypes() { return _eventTypes; }

	bool IAggregate.HasUnsavedEvents() { return _events.Count > 0; }

	IEnumerable<IEvent> IAggregate.GetUnsavedEvents() { return _events.ToArray(); }

	bool IAggregate.CanApplyEvent(IEvent aggregateEvent) { return _eventTypes.Contains(aggregateEvent.GetType()); }

	void IAggregate.ClearUnsavedEvents(int? upToVersion = null) {
		var unsavedEventCount = _events.Count;
		if (upToVersion.HasValue) {
			_events = new ConcurrentQueue<IEvent>(
				_events.Where(m => m.Details.AggregateVersion > upToVersion)
			);
		}
		else {
			_events.Clear();
		}

		unsavedEventCount -= _events.Count;

		Details.CurrentVersion -= unsavedEventCount;
	}

	void RecordAndApplyEvent(IEvent @event) {
		RecordEvent(@event);
		ApplyEvent(@event);
	}

	void IAggregate.ApplyEvent(IEvent @event) {
		if (Details.Locked) {
			throw new LockedException(Details.Id);
		}

		if (@event.Details.AggregateVersion == 1) {
			Details.Created = @event.Details.When;
		}

		Details.Updated = @event.Details.When;
		Details.CurrentVersion = @event.Details.AggregateVersion;

		if (@event is StringValueEvent) {
			ApplyEvent((StringValueEvent)@event);
		}
		else if (@event is IntValueEvent) {
			ApplyEvent((IntValueEvent)@event);
		}
		else if (@event is CustomEvent) {
			ApplyACustomEvent((CustomEvent)@event);
		}
		else if (@event is DeleteEvent) {
			ApplySystemEvent((DeleteEvent)@event);
		}
		else if (@event is RestoreEvent) {
			ApplySystemEvent((RestoreEvent)@event);
		}
		else if (@event is ForceSaveEvent) {
			ApplySystemEvent((ForceSaveEvent)@event);
		}
	}

	void RecordEvent(IEvent @event) {
		if (Details.Locked) {
			throw new AggregateLockedException(Details.Id);
		}

		if (@event.Details == null) {
			@event.Details = new AggregateEventDetails();
		}

		@event.Details.AggregateVersion = Details.CurrentVersion + 1;
		@event.Details.When = DateTimeOffset.UtcNow;

		_events.Enqueue(@event);
	}

	void ApplySystemEvent(DeleteEvent @event) {
		Details.IsDeleted = true;
	}

	void ApplySystemEvent(RestoreEvent @event) {
		Details.IsDeleted = false;
	}

	void ApplySystemEvent(ForceSaveEvent _) {
	}
}

// StringValue specific

partial class X {
	public string? StringValue {
		get => _stringValue;
		private set {
			if (Comparer<string?>.Default.Compare(value, _stringValue) != 0) {
				RecordAndApplyEvent(new StringValueEvent() { StringValue = value });
			}
		}
	}

	// Generate if it doesn't exist... need to find the name of the method.
	// Match method with parameter type.
	void ApplyEvent(StringValueEvent @event) {
		_stringValue = @event.StringValue;
	}
}

sealed public class StringValueEvent : IEvent {
	public AggregateEventDetails Details { get; set; } = new AggregateEventDetails();

	public string? StringValue { get; set; }

	override public int GetHashCode() {
		HashCode hashCode = new HashCode();

		hashCode.Add(StringValue);

		return hashCode.ToHashCode();
	}
}

// IntValue specific

partial class X {
	public int IntValue {
		get => _intValue;
		set {
			if (Comparer<int>.Default.Compare(value, _intValue) != 0) {
				RecordAndApplyEvent(new IntValueEvent() { IntValue = value });
			}
		}
	}

	// IF it doesn't exist...?
	void ApplyEvent(IntValueEvent @event) {
		_intValue = @event.Value;
	}
}

sealed public class IntValueEvent : IEvent {
	public AggregateEventDetails Details { get; set; } = new AggregateEventDetails();

	public int IntValue { get; set; }

	override public int GetHashCode() {
		HashCode hashCode = new HashCode();

		hashCode.Add(IntValue);

		return hashCode.ToHashCode();
	}
}
