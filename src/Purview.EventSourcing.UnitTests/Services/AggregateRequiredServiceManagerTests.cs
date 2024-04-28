using Purview.EventSourcing.Aggregates.Test;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Purview.EventSourcing.Services;

public partial class AggregateRequiredServiceManagerTests
{

	static AggregateRequiredServiceManager CreateServiceManager(IServiceProvider serviceProvider)
		=> new(serviceProvider);

	static IServiceProvider CreateServiceProvider(ITestService? testService, ITestService2? testService2)
	{
		List<ServiceDefinition> serviceDefinitions = [];
		if (testService != null)
		{
			serviceDefinitions.Add(new(typeof(ITestService), testService));
		}

		if (testService2 != null)
		{
			serviceDefinitions.Add(new(typeof(ITestService2), testService2));
		}

		return SubstituteBuilder.ServiceProvider([.. serviceDefinitions]);
	}

	static ITestService CreateTestService()
		=> Substitute.For<ITestService>();

	static ITestService2 CreateTestService2()
		=> Substitute.For<ITestService2>();

	static T CreateAggregate<T>()
		where T : AggregateRequirementsTest, new()
		=> SubstituteBuilder.Aggregate<T>();

	class AggregateRequirementsTest : TestAggregate, IRequirement<ITestService>
	{
		public ITestService? TestService { get; set; }

		public void SetService(ITestService service)
		{
			TestService = service;
		}
	}

	class AggregateMultipleRequirementsTest : AggregateRequirementsTest, IRequirement<ITestService2>
	{
		public ITestService2? TestService2 { get; set; }

		public void SetService(ITestService2 service)
		{
			TestService2 = service;
		}
	}

	internal interface ITestService
	{
	}

	internal interface ITestService2
	{
	}
}
