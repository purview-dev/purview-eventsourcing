﻿using System.Security.Cryptography;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDb;

public partial class GenericMongoDbEventStoreTests<TAggregate>(MongoDbEventStoreFixture fixture) : IMongoDbEventStoreTests, IClassFixture<MongoDbEventStoreFixture>
	where TAggregate : class, IAggregateTest, new()
{
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
}
