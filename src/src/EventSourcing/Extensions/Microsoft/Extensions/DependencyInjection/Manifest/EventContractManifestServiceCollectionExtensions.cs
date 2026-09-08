using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Manifest;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the runtime event-contract manifest provider.
/// </summary>
public static class EventContractManifestServiceCollectionExtensions
{
	/// <summary>
	/// Registers the generated event-contract manifest (and an optional approved baseline) so runtime
	/// and Admin tooling can inspect the contract and its compatibility status.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="formatVersion">The manifest format version (typically <c>EventContractManifest.FormatVersion</c>).</param>
	/// <param name="json">The generated manifest JSON (typically <c>EventContractManifest.Json</c>).</param>
	/// <param name="baselineJson">
	/// The approved baseline manifest JSON, when one is committed. When supplied, the provider
	/// reports <see cref="EventContractCompatibilityStatus.Compatible"/> only when it matches.
	/// </param>
	/// <returns>The <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddEventContractManifest(
		this IServiceCollection services,
		int formatVersion,
		string json,
		string? baselineJson = null
	)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(json);

		services.TryAddSingleton<IEventContractManifestProvider>(
			new EventContractManifestProvider(formatVersion, json, baselineJson)
		);

		return services;
	}
}
