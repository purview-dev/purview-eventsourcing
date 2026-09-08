# Purview EventSourcing

Purview EventSourcing is a .NET event sourcing framework for building aggregate-based applications with provider-agnostic store facades, source-generated aggregates, transaction coordination, and storage packages for SQL Server, Azure Storage, MongoDB, and Azure Cosmos DB.

[![Release](https://github.com/purview-dev/eventsourcing/actions/workflows/release.yml/badge.svg)](https://github.com/purview-dev/eventsourcing/actions/workflows/release.yml)

## Why use it

- Build aggregates on top of `AggregateBase` and load/save them through `IEventStore`.
- Add queryable read models with `IQueryableEventStore` when you need filtering, paging, and list views.
- Generate aggregate event types and registration code from partial methods using the source generator support included in `Purview.EventSourcing`.
- Coordinate multi-aggregate saves through `IEventStoreTransactionFactory`.
- Swap storage providers without changing your application-facing aggregate APIs.

## Packages

| Package ID | Purpose | Project README |
| --- | --- | --- |
| `Purview.EventSourcing` | Core abstractions, aggregate types, facades, transactions, DI extensions, and source generation support | [`src/src/EventSourcing/Sdk/README.md`](src/src/EventSourcing/Sdk/README.md) |
| `Purview.EventSourcing.SqlServer` | Azure SQL / SQL Server event stream and queryable snapshot stores | [`src/src/SqlServer/Sdk/README.md`](src/src/SqlServer/Sdk/README.md) |
| `Purview.EventSourcing.Postgres` | PostgreSQL event stream and queryable snapshot stores | [`src/src/Postgres/Sdk/README.md`](src/src/Postgres/Sdk/README.md) |
| `Purview.EventSourcing.AzureStorage` | Azure Table / Blob event store | [`src/src/AzureStorage/Sdk/README.md`](src/src/AzureStorage/Sdk/README.md) |
| `Purview.EventSourcing.MongoDB` | MongoDB event stream and queryable snapshot stores | [`src/src/MongoDB/Sdk/README.md`](src/src/MongoDB/Sdk/README.md) |
| `Purview.EventSourcing.CosmosDb` | Azure Cosmos DB queryable snapshot store | [`src/src/CosmosDb/Sdk/README.md`](src/src/CosmosDb/Sdk/README.md) |
| `Purview.EventSourcing.InMemory` | In-memory event/snapshot store implementation for local and test scenarios | (see package source at `src/src/InMemory`) |
| `Purview.EventSourcing.Validation.FluentValidation` | `FluentValidation` adapter for aggregate save-time validation | (see package source at `src/src/Validation.FluentValidation`) |
| `Purview.EventSourcing.Validation.ZodSharp` | `ZodSharp` adapter for aggregate save-time validation | (see package source at `src/src/Validation.ZodSharp`) |

## Install the packages you need

```bash
dotnet add package Purview.EventSourcing
dotnet add package Purview.EventSourcing.SqlServer
```

Provider packages layer on top of the core `Purview.EventSourcing` package. Add only the providers required for your chosen persistence strategy.

### Validation adapters

```bash
dotnet add package Purview.EventSourcing.Validation.FluentValidation
dotnet add package Purview.EventSourcing.Validation.ZodSharp
```

### ZodSharp direct-reference requirement

If your project directly references `Purview.EventSourcing.Validation.ZodSharp` (project reference) and uses types from `ZodSharp`, you must include a direct package reference:

```xml
<PackageReference Include="ZodSharp" />
```

`Purview.EventSourcing.Validation.ZodSharp` now ships a build-time guard target (`ValidateZodSharpDirectReference`) via NuGet `buildTransitive` assets. If the direct `ZodSharp` reference is missing, the consumer build fails with remediation guidance instead of allowing a runtime assembly-load failure.

## Quick start

### 1. Define an aggregate

```csharp
using Purview.EventSourcing.Aggregates;

[Aggregate]
public partial class OrderAggregate : AggregateBase
{
    public string CustomerId { get; private set; } = default!;
    public decimal Total { get; private set; }

    [Event]
    public partial void CreateOrder(string customerId);

    [Event]
    public partial void AddLineItem(string productId, string productName, int quantity, decimal unitPrice);
}
```

`[Aggregate]` supports three inheritance paths:

- No declared base class: the generated partial type automatically inherits `AggregateBase`.
- Direct inheritance from `AggregateBase`.
- Transitive inheritance through one or more intermediate base classes.

### 2. Register storage

```csharp
builder.Services.AddSqlServerEventStore();
builder.Services.AddSqlServerSnapshotQueryableEventStore();
```

```json
{
  "ConnectionStrings": {
    "eventstore-sqlserver": "Server=.;Database=MyApp;Trusted_Connection=True;"
  }
}
```

### 3. Load and save through the provider-agnostic facade

```csharp
public sealed class OrderService(IEventStore store)
{
    public async Task PlaceOrderAsync(string orderId, string customerId, CancellationToken cancellationToken)
    {
        var order = await store.GetAsync<OrderAggregate>(orderId, cancellationToken)
            ?? await store.CreateAsync<OrderAggregate>(orderId, cancellationToken: cancellationToken);

        order.CreateOrder(customerId);
        order.AddLineItem("SKU-1", "Demo product", 1, 19.99m);

        await store.SaveAsync(order, cancellationToken);
    }
}
```

### 4. Query through a snapshot-backed facade

```csharp
public sealed class OrderQueries(IQueryableEventStore store)
{
    public Task<long> CountActiveOrdersAsync(CancellationToken cancellationToken) =>
        store.CountAsync<OrderAggregate>(o => !o.Details.IsDeleted, cancellationToken);
}
```

### 5. Coordinate multi-aggregate saves

```csharp
public sealed class CheckoutService(
    IEventStoreTransactionFactory transactionFactory,
    IQueryableEventStore store)
{
    public async Task<bool> CheckoutAsync(
        OrderAggregate order,
        InventoryAggregate inventory,
        CancellationToken cancellationToken)
    {
        await using var transaction = transactionFactory.Create();
        transaction.Enlist(order, store);
        transaction.Enlist(inventory, store);

        var result = await transaction.CommitAsync(cancellationToken);
        return result.Success;
    }
}
```

## Storage provider matrix

| Provider | Package | Registration API | Notes |
| --- | --- | --- | --- |
| Core only | `Purview.EventSourcing` | `AddNullQueryableEventStore()` | No persistent query store |
| Azure SQL / SQL Server | `Purview.EventSourcing.SqlServer` | `AddSqlServerEventStore()` and `AddSqlServerSnapshotQueryableEventStore()` | Separate event and snapshot implementations in one package |
| PostgreSQL | `Purview.EventSourcing.Postgres` | `AddPostgresEventStore()` and `AddPostgresSnapshotQueryableEventStore()` | Separate event and snapshot implementations in one package |
| Azure Table / Blob | `Purview.EventSourcing.AzureStorage` | `AddAzureStorageEventStore()` | Table events plus Blob support for large payloads and snapshots |
| MongoDB | `Purview.EventSourcing.MongoDB` | `AddMongoDBEventStore()` and `AddMongoDBSnapshotQueryableEventStore()` | Separate event and snapshot implementations in one package |
| Azure Cosmos DB snapshots | `Purview.EventSourcing.CosmosDb` | `AddCosmosDbSnapshotQueryableEventStore()` | Queryable snapshot store |

For SQL Server and Azure SQL schema, permissions, and event-versioning guidance, see [docs/wiki/SQL-Server-Guide.md](docs/wiki/SQL-Server-Guide.md).

## Sample application

The sample solution demonstrates how the framework is intended to be consumed:

- `EventSourcing.Samples.Web` uses the non-generic `IEventStore` and `IQueryableEventStore` facades.
- `EventSourcing.Samples.QuickStart` is a console app that demonstrates related aggregates, multi-aggregate transactions, and rollback-on-failure behavior without external infrastructure.
- `EventSourcing.Samples.AppHost` wires up SQL Server, Redis, Azurite, and the web app for Aspire-driven local runs.
- Sample services such as `CartCheckoutService`, `OrderFulfilmentService`, and `StockTransferService` demonstrate multi-aggregate workflows.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/src` | Packable framework packages and sample applications |
| `src/tests` | Unit, integration, and source generator test projects |
| `docs/wiki` | Wiki-style project documentation (`Home.md`, SQL Server guide, release flow, source-generator behaviors) |
| `Justfile` | Build, test, format, version, pack, and pipeline workflow definitions |
| `purview-build.json` | Shared `Purview.Build` pipeline configuration (restore, build, lint, tests, pack) |

## Development workflow

The repository uses the shared [`Purview.Build`](https://github.com/purview-dev/build) pipeline for the full PR/release cycle, and plain `dotnet`/`just` commands for focused local work:

```text
dotnet tool restore
just pipeline-pr              # restore, build, lint, unit tests, pack, and package validation
just pipeline-build           # restore, build, lint, pack, and package validation (no tests)
just build                    # dotnet build src/EventSourcing.slnx --configuration Release
just test                     # dotnet test with a TUnit tree-node filter
just lint-check               # csharpier check
```

Additional notes:

- `just` recipes in the `Justfile` wrap the same restore, build, test, pack, and version commands for local development.
- `just pipeline-pr` and `just pipeline-tests` run the unit test projects discovered under `src/tests` (`*UnitTests.csproj`); integration tests use Testcontainers and run locally via `just test` when Docker is available.
- `package.json` is the release version source of truth for builds and packages.
- `dotnet pack` or `just pack` writes packages to `artifacts/packages`.

## Release workflow

Releases are fully automated from `main`:

1. Update the package version with the repository release process (changesets).
2. Review the generated `CHANGELOG.md` and package version changes.
3. Merge to `main`; the `Release` workflow validates, builds, packs, and publishes through the shared `Purview.Build` pipeline.
4. The pipeline creates the `v<version>` tag and GitHub release, and pushes NuGet packages to nuget.org.

The release runs only when the `v<version>` tag does not already exist, so re-merging to `main` does not double-release. NuGet publishing uses the `NUGET__APIKEY` organization secret.

Do not create release tags manually.

## Documentation

- [Wiki home](docs/wiki/Home.md)
- [Guarantees and limitations](docs/wiki/Guarantees-and-Limitations.md)
- [Getting started](docs/wiki/Getting-Started.md)
- [Provider feature matrix](docs/wiki/Provider-Feature-Matrix.md)
- [Provider capabilities](docs/wiki/Provider-Capabilities.md)
- [Transaction guarantees](docs/wiki/Transaction-Guarantees.md)
- [Event contract manifest](docs/wiki/Event-Contract-Manifest.md)
- [Source generator behaviors](docs/wiki/Source-Generator-Behaviors.md)
- [Source generator code fixes](docs/wiki/Code-Fixes.md)
- [SQL Server event store guide](docs/wiki/SQL-Server-Guide.md)
- [Dependency guardrails](docs/wiki/Dependency-Guardrails.md)
- [Release flow](docs/wiki/Release-Flow.md)
- [Core package README](src/src/EventSourcing/Sdk/README.md)
- [SQL Server provider README](src/src/SqlServer/Sdk/README.md)
- [Postgres provider README](src/src/Postgres/Sdk/README.md)
- [Azure Storage provider README](src/src/AzureStorage/Sdk/README.md)
- [MongoDB provider README](src/src/MongoDB/Sdk/README.md)
- [Cosmos DB provider README](src/src/CosmosDb/Sdk/README.md)
