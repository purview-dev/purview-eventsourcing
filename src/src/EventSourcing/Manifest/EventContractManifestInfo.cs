namespace Purview.EventSourcing.Manifest;

/// <summary>
/// The runtime compatibility status of the event-contract manifest against a baseline.
/// </summary>
public enum EventContractCompatibilityStatus
{
	/// <summary>The manifest is not registered or no baseline was supplied, so no status is computed.</summary>
	NotConfigured = 0,

	/// <summary>The current manifest matches the approved baseline.</summary>
	Compatible = 1,

	/// <summary>The current manifest differs from the approved baseline.</summary>
	Incompatible = 2,
}

/// <summary>
/// The event-contract manifest and its runtime compatibility status.
/// </summary>
/// <param name="FormatVersion">The manifest format version.</param>
/// <param name="Json">The current event-contract manifest serialized as compact JSON.</param>
/// <param name="BaselineJson">The approved baseline manifest, when supplied.</param>
/// <param name="CompatibilityStatus">Whether the current manifest matches the baseline.</param>
public sealed record EventContractManifestInfo(
	int FormatVersion,
	string Json,
	string? BaselineJson,
	EventContractCompatibilityStatus CompatibilityStatus
);
