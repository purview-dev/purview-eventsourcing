namespace Purview.EventSourcing.Manifest;

/// <summary>
/// Provides the runtime event-contract manifest and its compatibility status. Applications that
/// enable manifest generation register the generated manifest (and optionally the committed
/// baseline) so Admin tooling can inspect it.
/// </summary>
public interface IEventContractManifestProvider
{
	/// <summary>Returns the current manifest and its runtime compatibility status.</summary>
	Task<EventContractManifestInfo> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IEventContractManifestProvider"/> backed by the generated manifest constant
/// and an optional approved baseline.
/// </summary>
public sealed class EventContractManifestProvider(int formatVersion, string json, string? baselineJson)
	: IEventContractManifestProvider
{
	/// <inheritdoc/>
	public Task<EventContractManifestInfo> GetAsync(CancellationToken cancellationToken)
	{
		var status =
			string.IsNullOrWhiteSpace(baselineJson) ? EventContractCompatibilityStatus.NotConfigured
			: string.Equals(Normalize(json), Normalize(baselineJson), StringComparison.Ordinal)
				? EventContractCompatibilityStatus.Compatible
			: EventContractCompatibilityStatus.Incompatible;

		return Task.FromResult(new EventContractManifestInfo(formatVersion, json, baselineJson, status));
	}

	static string Normalize(string value) => value?.Trim() ?? string.Empty;
}
