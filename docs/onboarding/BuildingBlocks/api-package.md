# CustomerClub.BuildingBlocks.Api

## Overview

`CustomerClub.BuildingBlocks.Api` is the shared API chassis for Customer Club microservices.

This package standardizes the HTTP/API behavior that should be consistent across all services exposing HTTP endpoints. It provides common conventions for:

* API registration
* ProblemDetails responses
* global exception handling
* validation error responses
* Result-to-HTTP mapping
* Swagger/OpenAPI setup
* API versioning setup

The goal of this package is to prevent every microservice from redefining its own API behavior, error response shape, validation response format, and Swagger setup.

---

## Why this chassis exists

In a microservices architecture, each service is independently developed and deployed. Without a shared API chassis, every team or developer may implement HTTP concerns differently:

* different error response formats
* different validation response structures
* inconsistent status code mapping
* duplicated Swagger setup
* duplicated exception handling
* inconsistent correlation/trace metadata in API responses

This package exists to keep API behavior predictable and consistent across services while still keeping business logic inside each service.

`CustomerClub.BuildingBlocks.Api` does not own business rules. It only owns the HTTP/API boundary behavior.

---

## Package responsibility

This package is responsible for API-facing cross-cutting concerns.

### Included responsibilities

* Registering common API services
* Registering `ProblemDetails`
* Registering global exception handling
* Formatting model validation errors
* Mapping application `Result` objects to HTTP responses
* Adding standard metadata to error responses
* Registering API versioning
* Registering Swagger/OpenAPI conventions
* Providing API pipeline conventions

### Not included responsibilities

This package must not contain:

* business logic
* domain entities
* application use cases
* database logic
* messaging logic
* outbox logic
* authorization policies specific to a business domain
* Member, Wallet, Point Generator-specific behavior
* service-specific DTOs

---

## Boundary with other Building Blocks

### Relationship with `CustomerClub.BuildingBlocks.Application`

`Application` owns application-level result modeling:

* `Result`
* `Result<T>`
* `Error`
* `ErrorType`
* `ValidationError`

`Api` only translates those results to HTTP responses.

Example:

| Application result         | API response                |
| -------------------------- | --------------------------- |
| `Result.Success()`         | `204 No Content`            |
| `Result<T>.Success(value)` | `200 OK`                    |
| `ErrorType.Validation`     | `400 Bad Request`           |
| `ErrorType.NotFound`       | `404 Not Found`             |
| `ErrorType.Conflict`       | `409 Conflict`              |
| `ErrorType.Unauthorized`   | `401 Unauthorized`          |
| `ErrorType.Forbidden`      | `403 Forbidden`             |
| `ErrorType.Failure`        | `500 Internal Server Error` |
| `ErrorType.Unexpected`     | `500 Internal Server Error` |

Dependency direction:

```text
CustomerClub.BuildingBlocks.Api
  -> CustomerClub.BuildingBlocks.Application
```

The reverse dependency is not allowed.

`Application` must never reference `Api`.

---

### Relationship with `CustomerClub.BuildingBlocks.ServiceDefaults`

`ServiceDefaults` owns runtime defaults that are common to all service types:

* service identity
* health checks
* JSON defaults
* correlation middleware
* base runtime pipeline

`Api` owns HTTP/API-specific behavior:

* ProblemDetails
* exception-to-HTTP mapping
* Swagger
* API versioning
* validation response formatting
* Result-to-HTTP mapping

Recommended usage:

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

Do not hide API conventions inside `ServiceDefaults`.

Reason: not every service is necessarily an HTTP API. Workers, consumers, and background services may need `ServiceDefaults` but not `Api`.

---

### Relationship with `CustomerClub.BuildingBlocks.Observability`

`Observability` defines tracing/correlation conventions.

`Api` uses those conventions when adding metadata to HTTP error responses.

Example metadata added to `ProblemDetails`:

```json
{
  "traceId": "...",
  "correlationId": "...",
  "service": "member-service",
  "errorCode": "member.not_found"
}
```

`Api` should not implement full observability. It should only expose observability metadata at the HTTP boundary.

---

### Relationship with `CustomerClub.BuildingBlocks.Security`

`Security` owns claims, authentication, authorization helpers, current user context, and permission-related concerns.

`Api` may use security outputs if needed, but it should not contain security policy definitions specific to a domain.

Examples that do not belong in `Api`:

* `CanDebitWallet`
* `CanManageCampaign`
* `CanCreatePointRule`
* role/permission definitions for specific bounded contexts

---

## Main components

## 1. `AddCustomerClubApiConventions`

Registers API-specific services and conventions.

Typical responsibilities:

* configure `CustomerClubApiOptions`
* register global exception handling
* register `ProblemDetails`
* configure model validation response shape
* register API explorer
* register API versioning
* register Swagger/OpenAPI conventions

Usage:

```csharp
builder.Services.AddCustomerClubApiConventions(options =>
{
    options.ServiceName = "member-service";
    options.ApiTitle = "Member Service API";
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});
```

---

## 2. `UseCustomerClubApiConventions`

Configures API-specific middleware.

Typical responsibilities:

* enable global exception handler
* enable Swagger UI in development

Usage:

```csharp
var app = builder.Build();

app.UseCustomerClubDefaultPipeline();
app.UseCustomerClubApiConventions();

app.MapControllers();

app.Run();
```

---

## 3. `CustomerClubApiOptions`

Defines API-level options for a service.

Current responsibilities:

* `ServiceName`
* `ApiTitle`
* `EnableSwagger`
* `EnableApiVersioning`
* `IncludeExceptionDetails`

Example:

```csharp
builder.Services.AddCustomerClubApiConventions(options =>
{
    options.ServiceName = "wallet-service";
    options.ApiTitle = "Wallet Service API";
    options.EnableSwagger = true;
    options.EnableApiVersioning = true;
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});
```

---

## 4. `GlobalExceptionHandler`

Handles exceptions that escape the request pipeline.

Expected business failures should not be thrown as exceptions. They should be returned as `Result` or `Result<T>` from the application layer.

The exception handler is only for:

* unexpected technical failures
* unhandled exceptions
* rare API boundary exceptions

Example unexpected exception response:

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "An unexpected error occurred.",
  "instance": "/api/v1/members/123",
  "traceId": "...",
  "correlationId": "...",
  "service": "member-service",
  "errorCode": "internal.unexpected_error"
}
```

If `IncludeExceptionDetails` is enabled, exception details may be included in the response. This should only be enabled in development environments.

---

## 5. `ApiException`

`ApiException` is an API-boundary exception type.

Use it sparingly.

Preferred approach:

```csharp
return Result<MemberDto>.Failure(MemberErrors.NotFound(memberId));
```

Avoid using exceptions for expected business outcomes:

```csharp
throw new NotFoundApiException(...); // avoid for normal business flow
```

Recommended usage of `ApiException`:

* API-specific boundary failures
* exceptional HTTP-related errors
* cases where throwing is unavoidable and the error is not a normal business result

---

## 6. Validation response formatting

Model validation failures are converted to a standardized `ProblemDetails` response.

Example response:

```json
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/members",
  "traceId": "...",
  "correlationId": "...",
  "service": "member-service",
  "errorCode": "validation.failed",
  "errors": [
    {
      "field": "mobile",
      "message": "Mobile number is required.",
      "code": null
    }
  ]
}
```

Validation can happen in two places:

1. ASP.NET model validation before entering the action
2. application validation inside handlers or pipelines

Both paths should produce the same response shape.

---

## 7. `ResultHttpExtensions`

Maps application results to HTTP responses.

Controller usage:

```csharp
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
{
    var result = await handler.Handle(new GetMemberByIdQuery(id), cancellationToken);

    return result.ToActionResult(this);
}
```

Minimal API usage:

```csharp
app.MapGet("api/v1/members/{id:guid}", async (
    Guid id,
    HttpContext httpContext,
    IMemberQueries queries,
    CancellationToken cancellationToken) =>
{
    var result = await queries.GetByIdAsync(id, cancellationToken);

    return result.ToHttpResult(httpContext);
});
```

Expected mapping:

```text
Result.Success()              -> 204 No Content
Result<T>.Success(value)      -> 200 OK
ErrorType.Validation          -> 400 Bad Request
ErrorType.NotFound            -> 404 Not Found
ErrorType.Conflict            -> 409 Conflict
ErrorType.Unauthorized        -> 401 Unauthorized
ErrorType.Forbidden           -> 403 Forbidden
ErrorType.Failure             -> 500 Internal Server Error
ErrorType.Unexpected          -> 500 Internal Server Error
```

---

## Request flow

## Successful request

```text
Request
  -> ServiceDefaults pipeline
  -> API pipeline
  -> Controller / Minimal API
  -> Application handler
  -> Result<T>.Success(value)
  -> ResultHttpExtensions
  -> 200 OK
```

For command-like operations with no response body:

```text
Result.Success()
  -> 204 No Content
```

---

## Business error request

```text
Request
  -> Controller / Minimal API
  -> Application handler
  -> Result<T>.Failure(Error.NotFound(...))
  -> ResultHttpExtensions
  -> ProblemDetails
  -> 404 Not Found
```

Example:

```csharp
return Result<MemberDto>.Failure(
    Error.NotFound(
        "member.not_found",
        $"Member with id '{memberId}' was not found."));
```

---

## Validation error request

```text
Request
  -> Model validation or application validation
  -> Validation errors
  -> ProblemDetails + errors[]
  -> 400 Bad Request
```

Application-level validation example:

```csharp
return Result<MemberDto>.ValidationFailure([
    new ValidationError("mobile", "Mobile number is invalid.", "member.mobile_invalid"),
    new ValidationError("nationalCode", "National code is invalid.", "member.national_code_invalid")
]);
```

---

## Unexpected exception request

```text
Request
  -> Controller / Application handler
  -> Exception thrown
  -> GlobalExceptionHandler
  -> ProblemDetails
  -> 500 Internal Server Error
```

Expected business failures must not use this flow.

---

## Usage in a microservice

Recommended `Program.cs`:

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

## Design rules

## Rule 1: Do not put business logic here

This package must never know about:

* Member rules
* Wallet rules
* Point calculation rules
* Campaign rules
* domain aggregates
* database entities

---

## Rule 2: Do not throw exceptions for expected business errors

Preferred:

```csharp
return Result.Failure(Error.Conflict("wallet.insufficient_balance", "Wallet balance is insufficient."));
```

Avoid:

```csharp
throw new Exception("Wallet balance is insufficient.");
```

---

## Rule 3: Keep HTTP mapping in `Api`

`Application` should not know HTTP status codes.

Good:

```text
Application -> ErrorType.NotFound
Api         -> 404 Not Found
```

Bad:

```text
Application -> StatusCodes.Status404NotFound
```

---

## Rule 4: Keep `ServiceDefaults` separate from `Api`

Do not call `AddCustomerClubApiConventions` inside `AddCustomerClubServiceDefaults`.

Reason: not every service is an HTTP API.

Correct:

```csharp
builder.Services.AddCustomerClubServiceDefaults("member-service");
builder.Services.AddCustomerClubApiConventions(...);
```

---

## Future boundaries

The following features may be added to this package in the future:

* better Swagger document versioning
* standard API response examples
* common OpenAPI security schemes
* operation filters for correlation headers
* standardized deprecation metadata
* endpoint group conventions
* API version sunset policy
* standardized validation problem factory

The following features should not be added to this package:

* MediatR behaviors
* database transaction behaviors
* outbox publishing
* message broker configuration
* domain event publishing
* business validation rules
* authorization policies for specific services
* current user business access rules

---

## Package dependency rules

Allowed dependencies:

```text
CustomerClub.BuildingBlocks.Api
  -> CustomerClub.BuildingBlocks.Application
  -> CustomerClub.BuildingBlocks.Observability
```

Optional dependency depending on future design:

```text
CustomerClub.BuildingBlocks.Api
  -> CustomerClub.BuildingBlocks.Security
```

Not allowed:

```text
CustomerClub.BuildingBlocks.Application
  -> CustomerClub.BuildingBlocks.Api

CustomerClub.BuildingBlocks.Domain
  -> CustomerClub.BuildingBlocks.Api

CustomerClub.BuildingBlocks.Persistence
  -> CustomerClub.BuildingBlocks.Api
```

---

## Summary

`CustomerClub.BuildingBlocks.Api` is the HTTP boundary chassis for Customer Club microservices.

It standardizes:

* API setup
* exception handling
* validation responses
* ProblemDetails format
* result-to-HTTP mapping
* Swagger/OpenAPI setup
* API versioning

It must stay focused on API concerns only.

Business logic belongs to services.
Application result modeling belongs to `CustomerClub.BuildingBlocks.Application`.
Runtime defaults belong to `CustomerClub.BuildingBlocks.ServiceDefaults`.
Observability conventions belong to `CustomerClub.BuildingBlocks.Observability`.
