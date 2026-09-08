using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Purview.EventSourcing.Admin.Abstractions.Models;
using Purview.EventSourcing.Admin.Abstractions.Services;
using Purview.EventSourcing.Admin.Security.Handlers;
using Purview.EventSourcing.Admin.Security.Providers;
using Purview.EventSourcing.Admin.Security.Requirements;

namespace Purview.EventSourcing.Admin.Security;

/// <summary>
/// Registers the admin portal security services and authorization policies.
/// </summary>
public static class AdminSecurityServiceCollectionExtensions
{
	/// <summary>
	/// Registers the admin portal permission provider and authorization handlers with the service collection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <param name="permissionProvider">
	/// The permission provider to use, or <see langword="null"/> to register a deny-by-default provider.
	/// </param>
	/// <returns>The configured service collection.</returns>
	public static IServiceCollection AddPurviewEventSourcingAdminSecurity(
		this IServiceCollection services,
		IAdminPermissionProvider? permissionProvider = null
	)
	{
		// Deny-by-default if no provider supplied
		services.AddSingleton(permissionProvider ?? new DenyAllPermissionProvider());

		// Register authorization handlers
		services.AddScoped<IAuthorizationHandler, AdminFeatureAuthorizationHandler>();
		services.AddScoped<IAuthorizationHandler, AggregateTypeAccessHandler>();

		// Default in-memory audit logger; applications may replace it with a durable implementation.
		services.TryAddSingleton<IAdminAuditLogger, InMemoryAdminAuditLogger>();

		return services;
	}

	/// <summary>
	/// Adds the admin portal authorization policies to the authorization builder.
	/// </summary>
	/// <param name="builder">The authorization builder to configure.</param>
	/// <returns>The configured authorization builder for chaining.</returns>
	public static AuthorizationBuilder AddPurviewEventSourcingAdminPolicies([NotNull] this AuthorizationBuilder builder)
	{
		builder.AddPolicy(
			AdminPortalPolicies.SearchAggregates,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.SearchAggregates))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewAggregate,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewAggregate),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewEvents,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewEvents),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewEventPayloads,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.ViewEventPayloads))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ProjectPointInTime,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ProjectPointInTime),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ExportEvents,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ExportEvents),
					new AdminFeatureRequirement(AdminFeature.ViewEventPayloads),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewCapabilities,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.ViewCapabilities))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewPoisonedOutbox,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.ViewPoisonedOutbox))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewManifest,
			policy => policy.AddRequirements(new AdminFeatureRequirement(AdminFeature.ViewManifest))
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewUnknownEvents,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewUnknownEvents),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.ViewSnapshot,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.ViewSnapshot),
					new AggregateTypeAccessRequirement()
				)
		);

		builder.AddPolicy(
			AdminPortalPolicies.RebuildSnapshot,
			policy =>
				policy.AddRequirements(
					new AdminFeatureRequirement(AdminFeature.RebuildSnapshot),
					new AggregateTypeAccessRequirement()
				)
		);

		return builder;
	}
}
