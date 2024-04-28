﻿namespace Purview.EventSourcing;

/// <summary>
/// Options to influence Event Store operations.
/// </summary>
[System.Diagnostics.DebuggerStepThrough]
public record class EventStoreOperationContext
{
	static EventStoreOperationContext _default = new();

	/// <summary>
	/// Get or sets the default <see cref="EventStoreOperationContext"/>, for
	/// when <see cref="IEventStore{T}"/> operations provide a null operational context.
	/// </summary>
	public static EventStoreOperationContext DefaultContext
	{
		get => _default;
		set => _default = value ?? throw new NullReferenceException($"A default {nameof(EventStoreOperationContext)} is required, null is not allowed.");
	}

	/// <summary>
	/// Gets/ sets the default value for <see cref="RequiredValidPrincipalIdentifier"/>.
	/// </summary>
	public static bool RequiredValidPrincipalIdentifierDefault { get; set; } = true;

	/// <summary>
	/// Gets or sets the default value for the <see cref="ValidateIdempotencyMarker"/>. Defaults to false.
	/// </summary>
	public static bool ValidateIdempotencyMarkerDefault { get; set; }

	/// <summary>
	/// Controls how to handle aggregates marked as deleted during get and save/ restore operations.
	/// </summary>
	public DeleteHandlingMode DeleteMode { get; set; } = DeleteHandlingMode.ReturnsNull;

	/// <summary>
	/// Controls how to handle aggregates marked as locked.
	/// </summary>
	public LockHandlingMode LockMode { get; set; } = LockHandlingMode.ThrowsException;

	/// <summary>
	/// Manages caching operations for the event store.
	/// </summary>
	public EventStoreCacheOptions CacheOptions { get; set; } = new();

	/// <summary>
	/// <para>If true, the event store will skip getting the snapshot and instead attempt to reconstitute the aggregate entirely from it's events.</para>
	/// <para>Otherwise, if the <see cref="IEventStore{T}"/> supports snapshots then a pre-reconstituted snapshot may be retrieved.</para>
	/// </summary>
	/// <remarks>This can be a slow operation.</remarks>
	public bool SkipSnapshot { get; set; }

	/// <summary>
	/// Controls the notification mode when save, delete or restore operations occur.
	/// </summary>
	public NotificationModes NotificationMode { get; set; } = NotificationModes.All;

	/// <summary>
	/// <para>
	/// When false, aggregates are soft deleted. A platform event is added to mark the aggregate as deleted,
	/// and further save operations are forbidden. You can still get the aggregate by using the <see cref="DeleteMode"/> property
	/// and the <see cref="IEventStore{T}.RestoreAsync(T, EventStoreOperationContext?, CancellationToken)"/> operation.
	/// </para>
	/// <para>WARNING: If true, the aggregate and all of it's events and additional information are permanently deleted.</para>
	/// </summary>
	public bool PermanentlyDelete { get; set; }

	/// <summary>
	/// Validates the idempotency marker from <see cref="ICorrelationIdProvider"/> to see if the events have already been written.
	/// </summary>
	/// <remarks><para>If the marker exists, the call to <see cref="IEventStore{T}.SaveAsync(T, EventStoreOperationContext?, CancellationToken)"/> will return true without processing any of the events.</para>
	/// <para>In the case where this property is set to false, and the idempotency marker exists and internal exception is caught and ignored, and the operation requested will still set to true.</para>
	/// <para>If you're satisfied that the call is unique, then you can leave this as false (the default) for the sake of performance, otherwise set this to true.</para></remarks>
	public bool ValidateIdempotencyMarker { get; set; } = ValidateIdempotencyMarkerDefault;

	/// <summary>
	/// If true, a valid identifier must be returned from <see cref="IPrincipalService.Identifier()"/> or
	/// an exception is thrown.
	/// </summary>
	public bool RequiredValidPrincipalIdentifier { get; set; } = RequiredValidPrincipalIdentifierDefault;
}
