using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing;

static class TestHelpers
{
	public static IServiceProvider ServiceProvider(params ServiceDefinition[] serviceDefinitions)
	{
		var serviceProvider = Substitute.For<IServiceProvider>();
		if (serviceDefinitions != null)
		{
			foreach (var serviceDef in serviceDefinitions)
			{
				serviceProvider
					.GetService(Arg.Is(serviceDef.ServiceType))
					.Returns(serviceDef.Instance);
			}
		}

		return serviceProvider;
	}

	public static TAggregate Aggregate<TAggregate>(object? id = null, Action<TAggregate>? creator = null, bool clearEvents = true, bool clearIsNew = false)
		where TAggregate : class, IAggregate, new()
	{
		var aggregate = new TAggregate
		{
			Details =
			{
				Id = id?.ToString() ?? typeof(TAggregate).Name + $"_{Guid.NewGuid()}"
			}
		};

		creator?.Invoke(aggregate);

		if (clearEvents)
			aggregate.ClearUnsavedEvents(int.MaxValue);

		if (clearIsNew)
			aggregate.Details.SavedVersion = 1;

		return aggregate;
	}
}
