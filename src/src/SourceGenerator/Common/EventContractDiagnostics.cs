namespace Purview.EventSourcing.SourceGenerator.Common;

/// <summary>
/// Converts contract-compatibility issues into Roslyn diagnostics, resolving each issue to the
/// most specific current source location available (event method, then aggregate declaration).
/// </summary>
static class EventContractDiagnostics
{
	public static Diagnostic CreateDiagnostic(ContractIssue issue, EquatableArray<AggregateContractLocations> locations)
	{
		var location = ResolveLocation(issue, locations);
		return Diagnostic.Create(
			issue.Descriptor,
			ContractLocation.ToRoslynLocation(location),
			issue.MessageArgs.ToArray()
		);
	}

	static ContractLocation? ResolveLocation(ContractIssue issue, EquatableArray<AggregateContractLocations> locations)
	{
		foreach (var aggregate in locations)
		{
			if (issue.EventKey is not null)
			{
				foreach (var entry in aggregate.Entries)
				{
					if (StringComparer.Ordinal.Equals(entry.Key, issue.EventKey))
						return entry.Location;
				}
			}

			if (
				issue.AggregateKey is not null
				&& StringComparer.Ordinal.Equals(aggregate.AggregateKey, issue.AggregateKey)
			)
				return aggregate.Aggregate;
		}

		return null;
	}
}
