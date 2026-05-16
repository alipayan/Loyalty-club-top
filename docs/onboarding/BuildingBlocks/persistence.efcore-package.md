# CustomerClub.BuildingBlocks.Persistence.EfCore

## Overview

`CustomerClub.BuildingBlocks.Persistence.EfCore` is the EF Core implementation chassis for Customer Club microservices.

This package provides reusable EF Core implementations for persistence-related patterns that are common across services, especially:

- Outbox persistence
- Inbox persistence
- EF Core model configuration for Outbox/Inbox tables
- DI registration for EF-based persistence stores

This package exists to reduce repeated infrastructure code in every microservice while keeping the core `Persistence` package database-agnostic.

---

## Why this chassis exists

In a microservices architecture, each service owns its own database and infrastructure layer.

However, several persistence concerns are repeated across services:

- storing integration events in an Outbox table
- reading publishable Outbox messages
- marking messages as processing, published, failed, or dead-lettered
- storing processed Inbox messages for idempotent consumers
- configuring Outbox/Inbox tables and indexes in EF Core
- registering EF-based implementations of persistence abstractions

Without this chassis, every service would reimplement the same EF Core Outbox/Inbox logic.

This package centralizes that reusable EF Core implementation.

---

## Package responsibility

This package is responsible for EF Core-specific persistence implementations.

### Included responsibilities

- `EfCoreOutboxStore<TDbContext>`
- `EfCoreInboxStore<TDbContext>`
- Outbox EF model configuration
- Inbox EF model configuration
- DI extensions for registering EF Core persistence implementations
- reusable EF Core infrastructure helpers related to shared persistence patterns

### Not included responsibilities

This package must not contain:

- service-specific `DbContext`
- service-specific entities
- service-specific migrations
- connection strings
- domain entity configurations
- business repositories
- business rules
- message broker publishing
- event handler logic
- outbox publisher worker orchestration
- RabbitMQ/Kafka/MassTransit/Dapr setup

---

## Boundary with other Building Blocks

### Relationship with `CustomerClub.BuildingBlocks.Persistence`

`Persistence` is the core, database-agnostic package.

It owns shared persistence models and abstractions such as:

- `OutboxMessage`
- `OutboxMessageStatus`
- `IOutboxStore`
- `InboxMessage`
- `IInboxStore`
- transaction abstractions

`Persistence.EfCore` implements those abstractions using EF Core.

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

The core `Persistence` package must remain EF-free.

---

### Relationship with service Infrastructure layers

Each microservice owns its own infrastructure layer and its own `DbContext`.

Example:

```text
CustomerClub.Member.Infrastructure
  -> MemberDbContext
  -> Member entity configurations
  -> Member migrations
  -> references Persistence.EfCore
```

`Persistence.EfCore` does not define `MemberDbContext`, `WalletDbContext`, or `PointGeneratorDbContext`.

It only provides reusable EF Core implementations that each service can plug into its own `DbContext`.

---

### Relationship with `CustomerClub.BuildingBlocks.Messaging`

`Messaging` owns publishing and consuming abstractions.

`Persistence.EfCore` only stores and updates Outbox/Inbox records.

It does not publish messages to a broker.

Correct flow:

```text
Persistence.EfCore
  -> stores OutboxMessage

Messaging
  -> publishes EventEnvelope to broker
```

The bridge between these two belongs to an Outbox Publisher Worker or service-specific background process.

---

## Main components

### 1. `EfCoreOutboxStore<TDbContext>`

EF Core implementation of `IOutboxStore`.

It is responsible for:

- adding Outbox messages
- reading publishable messages
- marking messages as processing
- marking messages as published
- marking messages as failed
- dead-lettering messages after retry limit

Example registration:

```csharp
builder.Services.AddCustomerClubEfCoreOutbox<MemberDbContext>();
```

Or combined with Inbox:

```csharp
builder.Services.AddCustomerClubEfCorePersistence<MemberDbContext>();
```

---

### 2. `EfCoreInboxStore<TDbContext>`

EF Core implementation of `IInboxStore`.

It is responsible for idempotent event consumption.

It supports:

- checking whether an event was already processed by a specific consumer
- storing processed event records

Example:

```csharp
var alreadyProcessed = await inboxStore.HasProcessedAsync(
    envelope.EventId,
    consumer: "wallet-service.member-created-handler",
    cancellationToken);
```

If the event was already processed, the consumer should skip it.

---

### 3. Outbox model configuration

`AddCustomerClubOutbox` configures the EF Core model for Outbox messages.

Example usage in a service `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.AddCustomerClubOutbox(schema: "member");

    // service-specific configurations
}
```

This configuration defines:

- table name
- primary key
- required fields
- max lengths
- status conversion
- indexes for polling and diagnostics

---

### 4. Inbox model configuration

`AddCustomerClubInbox` configures the EF Core model for Inbox messages.

Example usage:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.AddCustomerClubInbox(schema: "member");

    // service-specific configurations
}
```

This configuration defines:

- table name
- primary key
- required fields
- unique index on `EventId + Consumer`
- indexes for cleanup and diagnostics

---

## Expected usage in a microservice

### 1. Register the service DbContext

```csharp
builder.Services.AddDbContext<MemberDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MemberDb"));
});
```

### 2. Register EF Core persistence chassis

```csharp
builder.Services.AddCustomerClubEfCorePersistence<MemberDbContext>();
```

Or only Outbox:

```csharp
builder.Services.AddCustomerClubEfCoreOutbox<MemberDbContext>();
```

Or only Inbox:

```csharp
builder.Services.AddCustomerClubEfCoreInbox<MemberDbContext>();
```

### 3. Add Outbox/Inbox tables to the service DbContext

```csharp
public sealed class MemberDbContext(DbContextOptions<MemberDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddCustomerClubOutbox(schema: "member");
        modelBuilder.AddCustomerClubInbox(schema: "member");

        // service-specific configurations
    }
}
```

### 4. Create migration inside the service

The migration belongs to the service, not to the Building Block.

Example:

```bash
dotnet ef migrations add AddOutboxInboxTables \
  --project src/Services/Member/src/CustomerClub.Member.Infrastructure \
  --startup-project src/Services/Member/src/CustomerClub.Member.Api
```

---

## Outbox flow

### Step 1: Application changes domain state

```text
Application Handler
  -> creates or updates domain entity
```

### Step 2: Add OutboxMessage in the same transaction

```csharp
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
```

### Step 3: Commit service database transaction

```csharp
await dbContext.SaveChangesAsync(cancellationToken);
```

Important:

`IOutboxStore.AddAsync` must not call `SaveChangesAsync` internally.

The Outbox message must be saved in the same transaction as the service data.

---

## Outbox publisher flow

```text
Outbox Publisher Worker
  -> reads publishable Outbox messages
  -> marks one message as Processing
  -> publishes event through Messaging
  -> marks message as Published
```

If publishing fails:

```text
Outbox Publisher Worker
  -> marks message as Failed
  -> increments retry count
  -> dead-letters after max retry count
```

---

## Inbox flow

Inbox is used to make event consumers idempotent.

```text
Consumer receives event
  -> checks Inbox by EventId + Consumer
  -> if already processed: skip
  -> handle event
  -> save local changes
  -> add InboxMessage
  -> commit transaction
```

Example:

```csharp
var alreadyProcessed = await inboxStore.HasProcessedAsync(
    envelope.EventId,
    consumerName,
    cancellationToken);

if (alreadyProcessed)
{
    return;
}

// handle event

await inboxStore.AddAsync(new InboxMessage
{
    Id = Guid.NewGuid(),
    EventId = envelope.EventId,
    EventType = envelope.EventType,
    Consumer = consumerName,
    ProcessedOnUtc = DateTimeOffset.UtcNow,
    CorrelationId = envelope.CorrelationId
}, cancellationToken);

await dbContext.SaveChangesAsync(cancellationToken);
```

---

## Design rules

### Rule 1: Keep EF Core implementation here, not in each service

Good:

```text
Persistence.EfCore
  -> EfCoreOutboxStore<TDbContext>
  -> EfCoreInboxStore<TDbContext>
```

Avoid duplicating the same Outbox/Inbox implementation in every microservice.

---

### Rule 2: Keep service DbContext inside the service

Good:

```text
Member.Infrastructure
  -> MemberDbContext

Wallet.Infrastructure
  -> WalletDbContext

PointGenerator.Infrastructure
  -> PointGeneratorDbContext
```

Avoid:

```text
Persistence.EfCore
  -> MemberDbContext
  -> WalletDbContext
  -> PointGeneratorDbContext
```

---

### Rule 3: Do not publish messages from this package

`Persistence.EfCore` only persists Outbox/Inbox records.

It must not depend on:

- RabbitMQ
- Kafka
- MassTransit
- Dapr
- `IEventPublisher`

Publishing belongs to Messaging or an Outbox Publisher Worker.

---

### Rule 4: Do not call `SaveChangesAsync` when adding Outbox/Inbox records

For application-side Outbox writes, the service must control the transaction.

Good:

```csharp
await outboxStore.AddAsync(outboxMessage, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);
```

Avoid:

```csharp
await outboxStore.AddAsync(outboxMessage, cancellationToken);
// AddAsync internally calls SaveChangesAsync
```

Reason:

The domain changes and the Outbox message must be committed atomically.

---

### Rule 5: Outbox polling must be safe for concurrent workers

Outbox workers may run in multiple instances.

The store must support safe claiming of messages.

Expected behavior:

```text
Worker A reads message
Worker B reads same message
Only one worker successfully marks it as Processing
The other worker skips it
```

This is why `TryMarkAsProcessingAsync` exists.

---

### Rule 6: Inbox must enforce idempotency

Event delivery can be at-least-once.

That means consumers may receive the same event more than once.

Inbox must prevent duplicate side effects.

Recommended unique constraint:

```text
EventId + Consumer
```

---

## Production considerations

### Retry handling

Outbox messages should support retry count and failure state.

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
  -> Pending/Processing again
  -> DeadLettered after max retries
```

---

### Processing timeout

A message marked as `Processing` should have an expiration time.

If a worker crashes while processing a message, another worker should be able to pick it up after timeout.

Recommended fields:

```text
ProcessingStartedOnUtc
ProcessingExpiresOnUtc
```

---

### Indexing

Outbox table should be optimized for polling.

Recommended indexes:

```text
Status + OccurredOnUtc
ProcessingExpiresOnUtc
CorrelationId
EventType + EventVersion
```

Inbox table should be optimized for idempotency checks.

Recommended indexes:

```text
EventId + Consumer unique
ProcessedOnUtc
```

---

### Cleanup

Outbox and Inbox tables can grow quickly.

Recommended future cleanup jobs:

```text
delete or archive Published Outbox messages older than retention period
delete or archive Inbox messages older than retention period
keep DeadLettered messages for investigation
```

Retention policy should be decided per environment and compliance needs.

---

## Recommended future additions

This package may later include:

- cleanup helpers for old Outbox/Inbox records
- provider-specific optimizations
- SQL Server-specific locking strategy
- PostgreSQL-specific locking strategy
- EF Core interceptors for automatically capturing domain events
- transactional helpers
- migration helper scripts
- health check helpers for Outbox backlog size

---

## What should not be added in the future

Do not add:

- RabbitMQ/Kafka/MassTransit publishing
- service-specific DbContexts
- service-specific migrations
- domain-specific repositories
- business-specific queries
- application command handlers
- API controllers
- HTTP response mapping
- service-specific options

---

## Package dependency rules

Allowed:

```text
Persistence.EfCore
  -> Persistence
  -> Microsoft.EntityFrameworkCore
```

Not allowed:

```text
Persistence
  -> Persistence.EfCore

Persistence.EfCore
  -> Api

Persistence.EfCore
  -> Messaging.RabbitMQ

Persistence.EfCore
  -> Member.Infrastructure

Persistence.EfCore
  -> Wallet.Infrastructure
```

---

## Summary

`CustomerClub.BuildingBlocks.Persistence.EfCore` is the EF Core adapter for shared persistence patterns.

It reduces duplicate infrastructure code across microservices by providing:

- EF Core Outbox store
- EF Core Inbox store
- Outbox model configuration
- Inbox model configuration
- DI registration helpers

It must remain generic, EF-focused, and service-agnostic.

`Persistence` defines the persistence abstractions.

`Persistence.EfCore` implements those abstractions using EF Core.

Each microservice still owns its own DbContext, migrations, schema, and domain-specific persistence logic.