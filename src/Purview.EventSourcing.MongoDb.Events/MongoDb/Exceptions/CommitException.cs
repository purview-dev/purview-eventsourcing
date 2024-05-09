﻿namespace Purview.EventSourcing.MongoDb.Exceptions;

#pragma warning disable CA1032 // Implement standard exception constructors
public class CommitException(string aggregateId, string idempotencyId, int versionAttempted, int version, Exception exception)
	: Exception($"Failed to commit events.\n\tAggregateId: {aggregateId}\n\tIdempotencyId: {idempotencyId}\n\tVersionAttempted: {versionAttempted}\n\tVersionPresent: {version}", exception)
#pragma warning restore CA1032 // Implement standard exception constructors
{
	public string AggregateId { get; } = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));

	public int VersionAttempted { get; } = versionAttempted;

	public string IdempotencyId { get; } = idempotencyId ?? throw new ArgumentNullException(nameof(idempotencyId));

	public int Version { get; } = version;
}
