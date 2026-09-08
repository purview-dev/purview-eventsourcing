namespace Purview.EventSourcing.Admin.Security;

/// <summary>
/// Defines the authorization policy names used by the admin portal.
/// </summary>
public static class AdminPortalPolicies
{
	/// <summary>
	/// Policy that authorizes searching for aggregates.
	/// </summary>
	public const string SearchAggregates = "AdminPortal.Aggregates.Search";

	/// <summary>
	/// Policy that authorizes viewing a single aggregate.
	/// </summary>
	public const string ViewAggregate = "AdminPortal.Aggregates.View";

	/// <summary>
	/// Policy that authorizes viewing an aggregate's event history.
	/// </summary>
	public const string ViewEvents = "AdminPortal.Events.View";

	/// <summary>
	/// Policy that authorizes viewing serialized event payloads.
	/// </summary>
	public const string ViewEventPayloads = "AdminPortal.Events.Payload.View";

	/// <summary>
	/// Policy that authorizes projecting aggregate state at a point in time.
	/// </summary>
	public const string ProjectPointInTime = "AdminPortal.Projections.Execute";

	/// <summary>
	/// Policy that authorizes exporting events from an aggregate stream.
	/// </summary>
	public const string ExportEvents = "AdminPortal.Events.Export";

	/// <summary>
	/// Policy that authorizes viewing the event-store capability contract and operational health.
	/// </summary>
	public const string ViewCapabilities = "AdminPortal.Capabilities.View";

	/// <summary>
	/// Policy that authorizes viewing poisoned (dead-letter) transactional outbox messages.
	/// </summary>
	public const string ViewPoisonedOutbox = "AdminPortal.Outbox.Poisoned.View";

	/// <summary>
	/// Policy that authorizes viewing the runtime event-contract manifest and its compatibility status.
	/// </summary>
	public const string ViewManifest = "AdminPortal.Manifest.View";

	/// <summary>
	/// Policy that authorizes viewing stored event type names the runtime cannot resolve.
	/// </summary>
	public const string ViewUnknownEvents = "AdminPortal.Events.Unknown.View";

	/// <summary>
	/// Policy that authorizes viewing aggregate snapshot status.
	/// </summary>
	public const string ViewSnapshot = "AdminPortal.Snapshots.View";

	/// <summary>
	/// Policy that authorizes rebuilding an aggregate snapshot from its event stream.
	/// </summary>
	public const string RebuildSnapshot = "AdminPortal.Snapshots.Rebuild";
}
