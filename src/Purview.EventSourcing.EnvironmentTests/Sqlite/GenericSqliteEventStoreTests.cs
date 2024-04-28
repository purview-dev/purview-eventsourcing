using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Interfaces.Services;
using Purview.EventSourcing.Sqlite.Context;
using Purview.Interfaces.Identity;
using Purview.Interfaces.Tracking;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>(bool useInMemory) : ISqliteEventStoreTests, IAsyncDisposable, IDisposable
	where TAggregate : class, IAggregateTest, new()
{
	EventStoreContext _eventStoreContext = default!;
	ICorrelationIdProvider _correlationIdProvider = default!;

	bool _disposedValue;

	static TAggregate CreateAggregate(string? id = null, Action<TAggregate>? action = null)
		=> SubstituteBuilder.Aggregate<TAggregate>(id, action);

	SqliteEventStore<TAggregate> CreateEventStore(int correlationIdsToGenerate = 1)
	{
		Guid runId = Guid.NewGuid();
		string[] runIds = Enumerable.Range(1, correlationIdsToGenerate).Select(_ => Guid.NewGuid().ToLoweredString()).ToArray();

		_correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
		_correlationIdProvider
			.GetCorrelationId()
			.Returns(runId.ToLoweredString(), runIds);

		string aggregateName = new TAggregate().AggregateType;

		IAggregateRequirementsManager requirementsManager = Substitute.For<IAggregateRequirementsManager>();

		_eventStoreContext = new EventStoreContext($"{aggregateName}_{runId}", useInMemory);

		IDbContextFactory<EventStoreContext> dbContextFactory = Substitute.For<IDbContextFactory<EventStoreContext>>();
		dbContextFactory
			.CreateDbContextAsync()
			.Returns(Task.Run(async () =>
			{
				await _eventStoreContext.Database.EnsureCreatedAsync();

				return _eventStoreContext;
			}));

		SqliteEventStore<TAggregate> eventStore = new(
			dbContextFactory: dbContextFactory,
			principalService: CreatePrincipalService(),
			correlationIdProvider: _correlationIdProvider,
			requirementsManager: requirementsManager
		);

		return eventStore;
	}

	static IPrincipalService CreatePrincipalService() => Substitute.For<IPrincipalService>();

	static ComplexTestType CreateComplexTestType()
	{
		return new()
		{
			Int16Property = (short)RandomNumberGenerator.GetInt32(short.MinValue, short.MaxValue),
			Int32Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue),
			Int64Property = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue) * 5L,
			StringProperty = $"{Guid.NewGuid()}",
			DateTimeOffsetProperty = DateTimeOffset.UtcNow.AddYears(RandomNumberGenerator.GetInt32(100, 1001)),
			ComplexNestedTestTypeProperty = new()
			{
				Nested = $"Nested_{Guid.NewGuid()}"
			}
		};
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_eventStoreContext?.Dispose();
				_eventStoreContext = null!;
			}

			_disposedValue = true;
		}
	}

	~GenericSqliteEventStoreTests()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore();

		Dispose(disposing: false);

		GC.SuppressFinalize(this);
	}

	protected async ValueTask DisposeAsyncCore()
	{
		if (_eventStoreContext != null)
		{
			await _eventStoreContext.DisposeAsync();
		}

		_eventStoreContext = null!;
	}
}
