# CustomerClub.BuildingBlocks.Persistence

## Overview

`CustomerClub.BuildingBlocks.Persistence` is the shared persistence abstraction chassis for Customer Club microservices.

This package defines common persistence models and contracts that are independent from any specific database technology.

It is intentionally database-agnostic and must stay independent from EF Core, SQL Server, PostgreSQL, MongoDB, RabbitMQ, Kafka, HTTP, and business-specific infrastructure.

The package currently has no external persistence dependency, which is the correct direction for this core chassis.

---

## Why this chassis exists

In a microservices architecture, each service owns its own database and persistence implementation.

However, some persistence concepts are repeated across services:

- Outbox messages
- Inbox messages
- idempotency records
- transaction boundaries
- pagination models
- persistence-level contracts

Without a shared persistence chassis, every service may define these concepts differently.

This package exists to standardize those shared persistence concepts while keeping the actual implementation inside service infrastructure or adapter packages such as `Persistence.EfCore`.

---

## Package responsibility

This package is responsible for database-agnostic persistence contracts and models.

### Included responsibilities

- Outbox message model
- Outbox store abstraction
- Inbox message model
- Inbox store abstraction
- transaction abstraction
- pagination primitives
- persistence-related options
- persistence-level shared contracts

### Not included responsibilities

This package must not contain:

- EF Core implementation
- DbContext
- migrations
- SQL Server-specific logic
- PostgreSQL-specific logic
- MongoDB-specific logic
- connection strings
- domain entities
- service-specific repositories
- RabbitMQ/Kafka/MassTransit publishing
- API controllers
- HTTP response mapping
- business rules

---

## Recommended structure

```text
CustomerClub.BuildingBlocks.Persistence
│
├── Outbox
│   ├── OutboxMessage.cs
│   ├── OutboxMessageStatus.cs
│   ├── OutboxOptions.cs
│   └── IOutboxStore.cs
│
├── Inbox
│   ├── InboxMessage.cs
│   ├── InboxOptions.cs
│   └── IInboxStore.cs
│
├── Transactions
│   └── IUnitOfWork.cs
│
├── Pagination
│   ├── PageRequest.cs
│   └── PageResult.cs
│
└── README.md
```

---

## Main components

### 1. Outbox

Outbox is used to reliably publish integration events after database changes are committed.

The core idea:

```text
Save domain data
  -> Save OutboxMessage in the same database transaction
  -> Commit transaction
  -> Background worker publishes OutboxMessage
```

This avoids losing events when the database transaction succeeds but broker publishing fails.

---

#### `OutboxMessage`

Represents an integration event waiting to be published.

Recommended shape:

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public required string EventType { get; set; }

    public required string EventVersion { get; set; }

    public required string Payload { get; set; }

    public string ContentType { get; set; } = "application/json";

    public required string Producer { get; set; }

    public DateTimeOffset OccurredOnUtc { get; set; }

    public DateTimeOffset? PublishedOnUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public string? TenantOrClubId { get; set; }

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LastAttemptOnUtc { get; set; }

    public DateTimeOffset? ProcessingStartedOnUtc { get; set; }

    public DateTimeOffset? ProcessingExpiresOnUtc { get; set; }
}
```

---

#### `OutboxMessageStatus`

Represents the lifecycle of an Outbox message.

```csharp
public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Published = 2,
    Failed = 3,
    DeadLettered = 4
}
```

Recommended lifecycle:

```text
Pending
  -> Processing
  -> Published
```

Failure path:

```text
Processing
  -> Failed
  -> Processing again
  -> DeadLettered after max retry count
```

---

#### `IOutboxStore`

Defines the storage contract for Outbox messages.

```csharp
public interface IOutboxStore
{
    Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPublishableAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        int maxRetryCount,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkAsProcessingAsync(
        Guid messageId,
        DateTimeOffset startedOnUtc,
        DateTimeOffset expiresOnUtc,
        CancellationToken cancellationToken = default);

    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedOnUtc,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset failedOnUtc,
        int maxRetryCount,
        CancellationToken cancellationToken = default);
}
```

Important:

`IOutboxStore.AddAsync` must not call `SaveChangesAsync` internally.

The service must control the transaction so domain changes and Outbox messages are committed atomically.

---

#### `OutboxOptions`

Controls Outbox publishing behavior.

```csharp
public sealed class OutboxOptions
{
    public int BatchSize { get; set; } = 50;

    public int MaxRetryCount { get; set; } = 5;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
```

---

### 2. Inbox

Inbox is used to make message consumers idempotent.

In distributed messaging, delivery is usually at-least-once. That means a consumer may receive the same event more than once.

Inbox prevents duplicate side effects.

Recommended flow:

```text
Consumer receives event
  -> Check Inbox by EventId + Consumer
  -> If already processed: skip
  -> Handle event
  -> Save local changes
  -> Save InboxMessage
  -> Commit transaction
```

---

#### `InboxMessage`

Represents a processed event.

```csharp
public sealed class InboxMessage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public required string EventType { get; set; }

    public required string Consumer { get; set; }

    public DateTimeOffset ProcessedOnUtc { get; set; }

    public string? CorrelationId { get; set; }
}
```

---

#### `IInboxStore`

Defines the storage contract for idempotent event processing.

```csharp
public interface IInboxStore
{
    Task<bool> HasProcessedAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);
}
```

---

#### `InboxOptions`

Controls Inbox behavior.

```csharp
public sealed class InboxOptions
{
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}
```

---

### 3. Transaction abstraction

#### `IUnitOfWork`

Defines a minimal transaction boundary abstraction.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Implementation belongs to the service infrastructure layer.

Example:

```csharp
public sealed class MemberUnitOfWork(MemberDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
```

This abstraction is intentionally minimal.

Do not add service-specific transaction behavior to this package.

---

### 4. Pagination

Pagination primitives can be shared when services need consistent paging behavior.

#### `PageRequest`

```csharp
public sealed record PageRequest(
    int PageNumber = 1,
    int PageSize = 20)
{
    public int Skip => (PageNumber - 1) * PageSize;
}
```

#### `PageResult<T>`

```csharp
public sealed record PageResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    long TotalCount)
{
    public long TotalPages =>
        PageSize <= 0 ? 0 : (long)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasNextPage => PageNumber < TotalPages;

    public bool HasPreviousPage => PageNumber > 1;
}
```

Do not add EF Core `IQueryable` extensions here.

If EF-specific pagination is needed, put it in:

```text
CustomerClub.BuildingBlocks.Persistence.EfCore
```

---

## Boundary with other Building Blocks

### Relationship with `CustomerClub.BuildingBlocks.Persistence.EfCore`

`Persistence` defines database-agnostic models and contracts.

`Persistence.EfCore` implements those contracts using EF Core.

Allowed dependency:

```text
CustomerClub.BuildingBlocks.Persistence.EfCore
  -> CustomerClub.BuildingBlocks.Persistence
```

Not allowed:

```text
CustomerClub.BuildingBlocks.Persistence
  -> CustomerClub.BuildingBlocks.Persistence.EfCore
```

Reason:

The core `Persistence` package must stay independent from EF Core.

---

### Relationship with service Infrastructure layers

Each service owns its infrastructure layer.

Example:

```text
CustomerClub.Member.Infrastructure
  -> MemberDbContext
  -> Member migrations
  -> Member entity configurations
  -> uses Persistence abstractions
  -> uses Persistence.EfCore implementation
```

`Persistence` must not contain service-specific infrastructure.

---

### Relationship with `CustomerClub.BuildingBlocks.Messaging`

`Messaging` owns event publishing and consuming abstractions.

`Persistence` owns Outbox/Inbox storage contracts.

They work together but must stay separate.

Correct responsibility split:

```text
Persistence
  -> stores OutboxMessage
  -> stores InboxMessage

Messaging
  -> publishes EventEnvelope
  -> consumes EventEnvelope
```

The bridge between them is usually an Outbox Publisher Worker.

---

### Relationship with `CustomerClub.BuildingBlocks.Application`

`Application` owns use-case outcomes such as:

- `Result`
- `Result<T>`
- `Error`
- `ValidationError`

`Persistence` must not depend on `Application`.

Not allowed:

```text
Persistence
  -> Result
  -> Error
  -> ErrorType
```

Persistence contracts should stay independent from application result modeling.

---

### Relationship with `CustomerClub.BuildingBlocks.Api`

`Api` owns HTTP behavior.

`Persistence` must not know about:

- controllers
- HTTP status codes
- `ProblemDetails`
- Swagger
- API versioning
- result-to-HTTP mapping

Not allowed:

```text
Persistence
  -> Api
```

---

## Expected usage in a microservice

### 1. Create domain state and Outbox message

```csharp
var member = Member.Create(command.Mobile);

dbContext.Members.Add(member);

var outboxMessage = new OutboxMessage
{
    Id = Guid.NewGuid(),
    EventType = "member.created",
    EventVersion = "v1",
    Payload = payloadJson,
    Producer = "member-service",
    OccurredOnUtc = DateTimeOffset.UtcNow,
    CorrelationId = correlationId,
    CausationId = null,
    TenantOrClubId = clubId
};

await outboxStore.AddAsync(outboxMessage, cancellationToken);

await unitOfWork.SaveChangesAsync(cancellationToken);
```

Both domain data and Outbox message must be committed together.

---

### 2. Publish Outbox messages in a background process

```text
Outbox Publisher Worker
  -> IOutboxStore.GetPublishableAsync
  -> IOutboxStore.TryMarkAsProcessingAsync
  -> IEventPublisher.PublishAsync
  -> IOutboxStore.MarkAsPublishedAsync
```

If publishing fails:

```text
Outbox Publisher Worker
  -> IOutboxStore.MarkAsFailedAsync
```

---

### 3. Consume events idempotently

```csharp
var alreadyProcessed = await inboxStore.HasProcessedAsync(
    envelope.EventId,
    consumerName,
    cancellationToken);

if (alreadyProcessed)
{
    return;
}

// handle event and update local database

await inboxStore.AddAsync(new InboxMessage
{
    Id = Guid.NewGuid(),
    EventId = envelope.EventId,
    EventType = envelope.EventType,
    Consumer = consumerName,
    ProcessedOnUtc = DateTimeOffset.UtcNow,
    CorrelationId = envelope.CorrelationId
}, cancellationToken);

await unitOfWork.SaveChangesAsync(cancellationToken);
```

---

## Design rules

### Rule 1: Keep this package database-agnostic

Good:

```text
OutboxMessage
IOutboxStore
InboxMessage
IInboxStore
IUnitOfWork
```

Avoid:

```text
DbContext
DbSet
EF Core configuration
SQL scripts
Mongo collections
provider-specific locking
```

---

### Rule 2: Do not implement stores here

`IOutboxStore` and `IInboxStore` belong here.

Implementations belong to adapter packages.

Example:

```text
CustomerClub.BuildingBlocks.Persistence.EfCore
  -> EfCoreOutboxStore<TDbContext>
  -> EfCoreInboxStore<TDbContext>
```

---

### Rule 3: Do not publish messages here

Persistence stores messages.

Messaging publishes messages.

Avoid:

```text
Persistence
  -> RabbitMQ
  -> Kafka
  -> MassTransit
  -> IEventPublisher
```

---

### Rule 4: Do not add business repositories

Avoid adding generic or business repositories such as:

```text
IMemberRepository
IWalletRepository
IPointRuleRepository
```

These belong to the owning service.

---

### Rule 5: Outbox must be written in the same transaction as domain data

Good:

```csharp
await outboxStore.AddAsync(outboxMessage, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

Avoid:

```csharp
await domainDbContext.SaveChangesAsync(cancellationToken);
await outboxStore.AddAsync(outboxMessage, cancellationToken);
await outboxStore.SaveChangesAsync(cancellationToken);
```

The second approach can create data/event inconsistency.

---

### Rule 6: Inbox must enforce idempotency

Consumers must assume duplicate messages can happen.

Inbox should be used to prevent duplicate side effects.

Recommended uniqueness rule:

```text
EventId + Consumer
```

---

## Production considerations

### Outbox retry

Outbox messages should support retry count and final failure state.

Recommended statuses:

```text
Pending
Processing
Published
Failed
DeadLettered
```

---

### Processing timeout

A message marked as `Processing` should expire if the worker crashes.

Recommended fields:

```text
ProcessingStartedOnUtc
ProcessingExpiresOnUtc
```

Expired processing messages can be picked up by another worker.

---

### Dead-lettering

After max retry count, messages should be marked as `DeadLettered`.

Dead-lettered messages should not be deleted automatically.

They should be kept for investigation.

---

### Cleanup

Outbox and Inbox tables can grow quickly.

Recommended cleanup strategy:

```text
Published Outbox messages older than retention period -> archive/delete
Inbox messages older than retention period -> archive/delete
DeadLettered messages -> keep for investigation
```

Cleanup implementation does not belong to the core `Persistence` package unless it is still database-agnostic.

Provider-specific cleanup belongs to adapter packages.

---

## Recommended future additions

This package may later include:

- retention option contracts
- cleanup abstraction
- audit field abstractions
- soft delete abstractions
- optimistic concurrency abstractions
- generic transaction execution abstraction
- domain event collection contracts, if needed

---

## What should not be added in the future

Do not add:

- EF Core implementation
- Dapper implementation
- SQL Server-specific locking
- PostgreSQL-specific locking
- MongoDB implementation
- RabbitMQ/Kafka publishing
- HTTP response mapping
- service-specific repositories
- domain entities
- service-specific query logic
- service-specific options

---

## Package dependency rules

Allowed:

```text
Persistence
  -> no database provider dependency
```

Allowed dependents:

```text
Persistence.EfCore
  -> Persistence

Service.Infrastructure
  -> Persistence
```

Not allowed:

```text
Persistence
  -> Persistence.EfCore

Persistence
  -> Api

Persistence
  -> Messaging.RabbitMQ

Persistence
  -> Member.Infrastructure
```

---

## Summary

`CustomerClub.BuildingBlocks.Persistence` is the database-agnostic persistence chassis for Customer Club microservices.

It standardizes:

- Outbox contracts
- Inbox contracts
- idempotency persistence contracts
- transaction boundary abstraction
- pagination primitives

It must remain:

- lightweight
- provider-independent
- service-agnostic
- free from EF Core and broker-specific dependencies

`Persistence` defines the contract.

`Persistence.EfCore` implements the contract using EF Core.

Each service owns its own database, migrations, DbContext, and domain-specific persistence logic.