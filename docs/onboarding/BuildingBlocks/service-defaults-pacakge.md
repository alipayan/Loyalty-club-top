# CustomerClub.BuildingBlocks.ServiceDefaults

## Overview

`CustomerClub.BuildingBlocks.ServiceDefaults` is the shared runtime chassis for Customer Club microservices.

This package provides the default runtime behavior that most services need, regardless of whether they are HTTP APIs, background workers, message consumers, or scheduled jobs.

It is responsible for service-level defaults such as:

- Service identity
- Basic health checks
- Default JSON configuration
- Correlation propagation
- Common runtime pipeline setup

The goal is to prevent each microservice from redefining the same runtime setup in its own `Program.cs`.

---

## Why this chassis exists

In a microservices architecture, each service is independently developed and deployed, but all services still need consistent runtime behavior.

Without a shared service defaults chassis, each service may implement these concerns differently:

- Different health check endpoints
- Different correlation header behavior
- Different JSON serialization settings
- Duplicated service identity setup
- Inconsistent runtime pipeline configuration

This package exists to keep the base runtime behavior consistent across all services.

It should remain lightweight, reusable, and independent from business logic.

---

## Current responsibilities

The current implementation provides:

- `AddCustomerClubServiceDefaults`
- `UseCustomerClubDefaultPipeline`
- Basic self health check
- JSON camelCase defaults
- `IHttpContextAccessor`
- `ServiceIdentity`
- Correlation header initialization
- `/health/live`
- `/health/ready`

Current package dependencies:

```text
CustomerClub.BuildingBlocks.ServiceDefaults
  -> Microsoft.AspNetCore.App
  -> CustomerClub.BuildingBlocks.Observability
```

---

## Package responsibility

This package is responsible for runtime-level cross-cutting concerns.

### Included responsibilities

- Registering service identity
- Registering basic health checks
- Mapping liveness and readiness endpoints
- Configuring basic JSON defaults
- Enabling access to current HTTP context when needed
- Ensuring correlation header exists
- Preparing common service runtime behavior

### Not included responsibilities

This package must not contain:

- Controllers
- Swagger/OpenAPI setup
- API versioning
- `ProblemDetails`
- Global exception-to-HTTP mapping
- Result-to-HTTP mapping
- Business validation
- Domain logic
- EF Core setup
- Outbox logic
- Message broker setup
- Service-specific authorization policies
- Member, Wallet, Point Generator-specific behavior

---

# Main components

## 1. `AddCustomerClubServiceDefaults`

Registers common runtime services.

Example:

```csharp
builder.Services.AddCustomerClubServiceDefaults("wallet-service");
```

This currently registers:

- Health checks
- JSON camelCase options
- `IHttpContextAccessor`
- `ServiceIdentity`

---

## 2. `UseCustomerClubDefaultPipeline`

Configures common runtime middleware and endpoints.

Current responsibilities:

- Ensures `x-correlation-id` exists
- Maps `/health/live`
- Maps `/health/ready`

Example:

```csharp
app.UseCustomerClubDefaultPipeline();
```

---

## 3. `ServiceIdentity`

Represents the current service identity.

Current shape:

```csharp
public sealed record ServiceIdentity(string Name);
```

Example usage:

```csharp
public sealed class SomeService(ServiceIdentity serviceIdentity)
{
    public string ServiceName => serviceIdentity.Name;
}
```

`ServiceIdentity` is useful for:

- Logging enrichment
- Telemetry metadata
- Diagnostics
- Service-specific runtime identification

---

## 4. Health checks

This package provides base health check endpoints.

Current endpoints:

```text
/health/live
/health/ready
```

### `/health/live`

Used to determine whether the process is alive.

This should be lightweight and should not depend on external infrastructure such as database, Redis, or broker.

### `/health/ready`

Used to determine whether the service is ready to receive traffic.

Infrastructure-specific readiness checks may be added later by each service or by other Building Blocks such as Persistence or Messaging.

Example future checks:

- Database connectivity
- Redis connectivity
- Message broker connectivity
- External dependency readiness

---

## 5. Correlation

This package ensures that a correlation header exists for each HTTP request.

Current correlation header comes from:

```text
CustomerClub.BuildingBlocks.Observability.ObservabilityConventions.CorrelationHeader
```

Current convention:

```text
x-correlation-id
```

If the request does not contain a correlation id, the pipeline uses `HttpContext.TraceIdentifier` as the default value.

Correlation is important for tracing one request across multiple services.

---

# Boundary with other Building Blocks

## Relationship with `CustomerClub.BuildingBlocks.Api`

`ServiceDefaults` owns runtime defaults.

`Api` owns HTTP API behavior.

Correct usage:

```csharp
builder.Services.AddCustomerClubServiceDefaults("member-service");

builder.Services.AddCustomerClubApiConventions(options =>
{
    options.ServiceName = "member-service";
    options.ApiTitle = "Member Service API";
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});
```

Pipeline:

```csharp
app.UseCustomerClubDefaultPipeline();
app.UseCustomerClubApiConventions();
```

Do not call `AddCustomerClubApiConventions` inside `AddCustomerClubServiceDefaults`.

Reason:

Not every service is an HTTP API. A background worker or message consumer may need `ServiceDefaults` but not `Api`.

---

## Relationship with `CustomerClub.BuildingBlocks.Observability`

`Observability` owns naming and telemetry conventions.

`ServiceDefaults` may use those conventions at runtime.

Example:

```text
Observability
  -> defines x-correlation-id

ServiceDefaults
  -> ensures x-correlation-id exists in the request pipeline
```

`ServiceDefaults` should not become a full observability implementation.

Full logging, metrics, tracing, exporters, and dashboards should be handled by `Observability` or dedicated infrastructure configuration.

---

## Relationship with `CustomerClub.BuildingBlocks.Application`

`Application` owns use-case outcomes such as:

- `Result`
- `Result<T>`
- `Error`
- `ErrorType`
- `ValidationError`

`ServiceDefaults` must not know about application results.

Not allowed:

```text
ServiceDefaults
  -> Result
  -> Error
  -> ErrorType
```

Result-to-HTTP mapping belongs to `CustomerClub.BuildingBlocks.Api`.

---

## Relationship with `CustomerClub.BuildingBlocks.Persistence`

`Persistence` owns database-related behavior.

`ServiceDefaults` must not contain:

- `DbContext` registration
- EF Core conventions
- Migrations
- Transaction handling
- Outbox persistence
- Database health check implementation specific to a provider

A service or `Persistence` Building Block may add readiness checks to the health check builder, but the base `ServiceDefaults` should stay database-agnostic.

---

## Relationship with `CustomerClub.BuildingBlocks.Messaging`

`Messaging` owns broker-related behavior.

`ServiceDefaults` must not contain:

- RabbitMQ/Kafka/MassTransit/Dapr setup
- Message publishing
- Message consuming
- Topic/queue configuration
- Broker-specific health checks

Messaging-specific readiness checks may be added by `Messaging` or the owning service.

---

# Expected usage in a microservice

## HTTP API service

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerClubServiceDefaults("member-service");

builder.Services.AddCustomerClubApiConventions(options =>
{
    options.ServiceName = "member-service";
    options.ApiTitle = "Member Service API";
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCustomerClubDefaultPipeline();
app.UseCustomerClubApiConventions();

app.MapControllers();

app.Run();
```

---

## Worker or background service

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerClubServiceDefaults("point-generator-worker");

builder.Services.AddHostedService<PointGenerationWorker>();

var app = builder.Build();

app.UseCustomerClubDefaultPipeline();

app.Run();
```

A worker may not need `CustomerClub.BuildingBlocks.Api`, but it can still use `ServiceDefaults` for health, identity, and correlation-related defaults.

---

## Request flow in an HTTP API

```text
Request
  -> ServiceDefaults pipeline
  -> correlation id ensured
  -> API pipeline
  -> controller / endpoint
  -> application handler
  -> response
```

Health check flow:

```text
GET /health/live
  -> ServiceDefaults health endpoint
  -> liveness result
```

```text
GET /health/ready
  -> ServiceDefaults health endpoint
  -> readiness result
```

---

# Design rules

## Rule 1: Keep this package runtime-focused

Good:

```text
health checks
service identity
correlation
JSON defaults
basic runtime middleware
```

Avoid:

```text
controllers
Swagger
ProblemDetails
EF Core
message brokers
business rules
```

---

## Rule 2: Do not hide API conventions inside ServiceDefaults

Good:

```csharp
builder.Services.AddCustomerClubServiceDefaults("member-service");
builder.Services.AddCustomerClubApiConventions(...);
```

Avoid:

```csharp
builder.Services.AddCustomerClubServiceDefaults("member-service");
// internally calls AddCustomerClubApiConventions
```

Keeping them separate makes the architecture clearer and allows non-HTTP services to use `ServiceDefaults`.

---

## Rule 3: Base health checks should stay lightweight

The default liveness check should only answer:

> Is the process alive?

Do not add database, Redis, or broker checks to the base liveness check.

Those dependencies belong to readiness checks and should be added by the owning service or the related Building Block.

---

## Rule 4: Keep dependency direction clean

Allowed:

```text
ServiceDefaults
  -> Observability
```

Avoid:

```text
ServiceDefaults
  -> Api
ServiceDefaults
  -> Application
ServiceDefaults
  -> Persistence
ServiceDefaults
  -> Messaging
```

`ServiceDefaults` should remain a low-level runtime package.

---

## Rule 5: Do not put business configuration here

Do not add service-specific settings such as:

```text
WalletOptions
PointRuleOptions
CampaignOptions
MemberRegistrationOptions
```

Those belong to the owning service.

---

# Recommended future structure

The current implementation can evolve into this structure:

```text
CustomerClub.BuildingBlocks.ServiceDefaults
│
├── Configuration
│   └── ServiceDefaultsOptions.cs
│
├── Correlation
│   └── CorrelationMiddleware.cs
│
├── Health
│   └── HealthCheckTags.cs
│
├── Identity
│   └── ServiceIdentity.cs
│
└── ServiceDefaultsExtensions.cs
```

---

# Recommended future additions

This package may later include:

- `ServiceDefaultsOptions`
- `HealthCheckTags`
- Dedicated `CorrelationMiddleware`
- Response correlation header
- Separated `ServiceIdentity` file
- Improved JSON defaults
- Default outbound `HttpClient` conventions
- Optional resilience defaults for outbound calls
- Graceful shutdown conventions
- Service metadata registration

---

## Future addition: `ServiceDefaultsOptions`

Recommended shape:

```csharp
public sealed class ServiceDefaultsOptions
{
    public string ServiceName { get; set; } = "unknown-service";

    public bool EnableHealthChecks { get; set; } = true;

    public bool EnableCorrelation { get; set; } = true;

    public bool EnableJsonDefaults { get; set; } = true;

    public bool EnableHttpContextAccessor { get; set; } = true;
}
```

Usage:

```csharp
builder.Services.AddCustomerClubServiceDefaults(options =>
{
    options.ServiceName = "member-service";
    options.EnableHealthChecks = true;
    options.EnableCorrelation = true;
});
```

---

## Future addition: health check tags

Avoid raw strings like `"live"` and `"ready"`.

Recommended:

```csharp
public static class HealthCheckTags
{
    public const string Live = "live";
    public const string Ready = "ready";
}
```

Usage:

```csharp
services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: [HealthCheckTags.Live]);
```

---

## Future addition: dedicated correlation middleware

Recommended behavior:

- Read `x-correlation-id` from the request
- Create one if missing
- Add it to response headers
- Make it available for logging/tracing

Example behavior:

```text
Request without x-correlation-id
  -> ServiceDefaults creates correlation id
  -> response contains x-correlation-id
```

```text
Request with x-correlation-id
  -> ServiceDefaults preserves it
  -> response contains the same x-correlation-id
```

---

# What should not be added in the future

Do not add:

- `AddCustomerClubApiConventions`
- `UseCustomerClubApiConventions`
- Swagger/OpenAPI configuration
- API versioning
- `ProblemDetails`
- `GlobalExceptionHandler`
- `ResultHttpExtensions`
- EF Core configuration
- Outbox implementation
- Message broker configuration
- Domain events
- Service-specific options
- Business rules
- Authorization policies specific to one bounded context

---

# Summary

`CustomerClub.BuildingBlocks.ServiceDefaults` is the shared runtime chassis for Customer Club microservices.

It standardizes:

- Service identity
- Health endpoints
- Correlation initialization
- JSON defaults
- Base runtime pipeline

It should remain:

- Lightweight
- Runtime-focused
- Service-agnostic
- Reusable by both HTTP services and non-HTTP services

```text
ServiceDefaults -> prepares the service runtime
Api             -> defines HTTP behavior
Application     -> models use-case outcomes
Persistence     -> owns database concerns
Messaging       -> owns broker concerns
```

Services own their business logic.