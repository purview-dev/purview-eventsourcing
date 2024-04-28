namespace Purview.EventSourcing.Sqlite;

partial class SqliteEventStoreTests
{
	public static TheoryData<bool, Type, int, int> SteppedAggregateCountWithDeletedAggregateIdCountTestData
	{
		get
		{
			TheoryData<bool, Type, int, int> data = [];
			foreach (Type aggregateType in AggregateTypes.Select(m => (Type)m[1]!))
			{
				foreach (bool useInMemory in new[] { true, false })
				{
					data.Add(useInMemory, aggregateType, 1, 1);
					data.Add(useInMemory, aggregateType, 1, 10);
					data.Add(useInMemory, aggregateType, 5, 5);
					data.Add(useInMemory, aggregateType, 5, 10);
					data.Add(useInMemory, aggregateType, 10, 10);
					data.Add(useInMemory, aggregateType, 10, 20);
					data.Add(useInMemory, aggregateType, 20, 20);
					data.Add(useInMemory, aggregateType, 20, 40);
				}
			}

			return data;
		}
	}

	public static TheoryData<bool, Type, int> SteppedCountTestData
	{
		get
		{
			TheoryData<bool, Type, int> data = [];
			foreach (Type aggregateType in AggregateTypes.Select(m => (Type)m[1]!))
			{
				foreach (bool useInMemory in new[] { true, false })
				{
					data.Add(useInMemory, aggregateType, 1);
					data.Add(useInMemory, aggregateType, 10);
					data.Add(useInMemory, aggregateType, 20);
					data.Add(useInMemory, aggregateType, 50);
				}
			}

			return data;
		}
	}

	public static TheoryData<bool, Type> AggregateTypes
	{
		get
		{
			TheoryData<bool, Type> data = [];

			Type[] aggregateTypes = [
				typeof(Aggregates.Persistence.PersistenceAggregate)
			];
			bool[] useInMemoryValues = [true, false];

			foreach (Type aggregateType in aggregateTypes)
			{
				foreach (bool useInMemory in useInMemoryValues)
				{
					data.Add(useInMemory, aggregateType);
				}
			}

			return data;
		}
	}

	static ISqliteEventStoreTests CreateTableStoreTests(bool useInMemory, Type aggregateType)
	{
		Type testType = typeof(GenericSqliteEventStoreTests<>).MakeGenericType(aggregateType);

		return (ISqliteEventStoreTests)Activator.CreateInstance(testType, [useInMemory])!;
	}
}
