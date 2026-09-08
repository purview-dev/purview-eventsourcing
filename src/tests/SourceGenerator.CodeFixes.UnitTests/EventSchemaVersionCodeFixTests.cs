using Purview.EventSourcing.SourceGenerator.Analyzers;

namespace Purview.EventSourcing.SourceGenerator.CodeFixes;

public sealed class EventSchemaVersionCodeFixTests
{
	const string NonPositiveVersion = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class OrderAggregate : AggregateBase
			{
				public string CustomerId { get; private set; }

				[Event(Version = 0)]
				public partial void CreateOrder(string customerId);
			}
		}
		""";

	const string NonPositiveVersionNamed = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class OrderAggregate : AggregateBase
			{
				public string CustomerId { get; private set; }

				[Event(Version = -3)]
				public partial void CreateOrder(string customerId);
			}
		}
		""";

	const string DuplicateSchemaVersion = """
		using Purview.EventSourcing.Aggregates;

		namespace Testing
		{
			[Aggregate]
			public partial class OrderAggregate : AggregateBase
			{
				public string CustomerId { get; private set; }
				public decimal Total { get; private set; }

				[Event(Version = 2)]
				public partial void CreateOrder(string customerId);

				[Event(Version = 2)]
				public partial void UpdateTotal(decimal total);
			}
		}
		""";

	[Test]
	public async Task GivenNonPositiveVersion_ResetsToVersionOne(CancellationToken cancellationToken)
	{
		var result = await CodeFixTestHarness.ApplyAsync<
			AggregateDiagnosticAnalyzer,
			EventSchemaVersionCodeFixProvider
		>(NonPositiveVersion, cancellationToken);

		await Assert.That(result.FixedCode).Contains("[Event(Version = 1)]");
	}

	[Test]
	public async Task GivenNegativeVersionNamedArgument_ResetsToVersionOne(CancellationToken cancellationToken)
	{
		var result = await CodeFixTestHarness.ApplyAsync<
			AggregateDiagnosticAnalyzer,
			EventSchemaVersionCodeFixProvider
		>(NonPositiveVersionNamed, cancellationToken);

		await Assert.That(result.FixedCode).Contains("[Event(Version = 1)]");
	}

	[Test]
	public async Task GivenDuplicateSchemaVersion_MovesToNextUnusedVersion(CancellationToken cancellationToken)
	{
		var result = await CodeFixTestHarness.ApplyAsync<
			AggregateDiagnosticAnalyzer,
			EventSchemaVersionCodeFixProvider
		>(DuplicateSchemaVersion, cancellationToken);

		var fixedCode = result.FixedCode;
		await Assert.That(fixedCode).Contains("[Event(Version = 3)]");
		await Assert.That(fixedCode).Contains("[Event(Version = 2)]");
	}
}
