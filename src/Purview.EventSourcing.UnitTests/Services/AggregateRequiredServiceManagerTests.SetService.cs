using Purview.EventSourcing.Aggregates.Test;

namespace Purview.EventSourcing.Services;

partial class AggregateRequiredServiceManagerTests
{
	[Fact]
	public void Populate_GivenAggregateHasIRequirement_PopulatesService()
	{
		// Arrange
		AggregateRequirementsTest aggregate = CreateAggregate<AggregateRequirementsTest>();
		ITestService testService = CreateTestService();
		IServiceProvider serviceProvider = CreateServiceProvider(testService, null);

		AggregateRequiredServiceManager serviceManager = CreateServiceManager(serviceProvider);

		// Act
		serviceManager.Fulfil(aggregate);

		// Assert
		aggregate.TestService.Should().BeSameAs(testService);
	}

	[Fact]
	public void Populate_GivenAggregateHasMultipleIRequirements_PopulatesServices()
	{
		// Arrange
		AggregateMultipleRequirementsTest aggregate = CreateAggregate<AggregateMultipleRequirementsTest>();

		ITestService testService = CreateTestService();
		ITestService2 testService2 = CreateTestService2();
		IServiceProvider serviceProvider = CreateServiceProvider(testService, testService2);

		AggregateRequiredServiceManager serviceManager = CreateServiceManager(serviceProvider);

		// Act
		serviceManager.Fulfil(aggregate);

		// Assert
		aggregate.TestService.Should().BeSameAs(testService);
		aggregate.TestService2.Should().BeSameAs(testService2);
	}

	[Fact]
	public void Populate_GivenAggregateHasNoIRequirements_DoesNotThrow()
	{
		// Arrange
		TestAggregate aggregate = SubstituteBuilder.Aggregate<TestAggregate>();
		IServiceProvider serviceProvider = CreateServiceProvider(null, null);

		AggregateRequiredServiceManager serviceManager = CreateServiceManager(serviceProvider);

		// Act
		Action act = () => serviceManager.Fulfil(aggregate);

		// Assert
		act.Should().NotThrow();
	}
}
