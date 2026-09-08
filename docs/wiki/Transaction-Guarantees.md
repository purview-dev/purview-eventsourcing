# Transaction Guarantees

`IEventStoreTransaction` coordinates aggregate saves, but the exact guarantee depends on the enlisted stores.
Callers can inspect `AvailableGuarantee` after enlistment and can require atomicity when creating a transaction.

```csharp
await using var transaction = transactionFactory.Create(
    new EventStoreTransactionOptions
    {
        CorrelationId = command.CorrelationId,
        RequiredGuarantee = EventStoreTransactionGuarantee.Atomic,
    });

transaction.Enlist(order, eventStore);
transaction.Enlist(inventory, eventStore);
await transaction.CommitAsync(cancellationToken);
```

When `Atomic` is required, `CommitAsync` validates every enlisted store and its native transaction boundary before
performing any write. Incompatible providers, different databases, or stores without a native coordinator cause an
`EventStoreTransactionGuaranteeException`; no enlisted aggregate is saved.

When `BestEffort` is accepted (the backward-compatible default), the coordinator uses a native atomic transaction
when every store shares a supported boundary. Otherwise, it saves sequentially under one correlation ID, stops on
the first failure, and does not roll back earlier saves. A correlation ID and idempotency marker aid recovery but do
not make sequential saves atomic.

| Scenario | Available guarantee |
| --- | --- |
| SQL Server stores sharing one configured database boundary | `Atomic` |
| PostgreSQL stores sharing one configured database boundary | `Atomic` |
| Stores using different database boundaries | `BestEffort` |
| Any provider without native transaction coordination | `BestEffort` |
| Mixed providers | `BestEffort` |

Provider-specific SQL transaction factories always require and provide `Atomic`; they reject unsupported stores
during enlistment. Provider-neutral transactions make the requirement explicit at creation and verify it again at
commit, after all stores have been enlisted.
