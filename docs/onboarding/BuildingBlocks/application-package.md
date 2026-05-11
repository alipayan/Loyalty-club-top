# CustomerClub.BuildingBlocks.Application

## Overview

`CustomerClub.BuildingBlocks.Application` is the shared application-layer chassis for Customer Club microservices.

This package provides common application-level primitives used by service use cases and handlers.

It is intentionally lightweight and must stay independent from HTTP, database, messaging, and infrastructure concerns.

---

## Why this chassis exists

In a microservices architecture, each service owns its own business logic, but all services still need a consistent way to express application outcomes.

This package standardizes the language for answering this question:

> Did the use case succeed, fail with an expected business error, or fail with validation errors?

Without this shared model, each service may define its own result, error, and validation shape differently.

---

## Package responsibility

This package is responsible for application-level primitives.

### Included responsibilities

- `Result`
- `Result<T>`
- `Error`
- `ErrorType`
- `ValidationError`
- Common application abstractions in the future

### Not included responsibilities

This package must not contain:

- HTTP status codes
- Controllers
- `ProblemDetails`
- Swagger/OpenAPI setup
- Exception-to-HTTP mapping
- Database logic
- EF Core logic
- Outbox logic
- Message broker logic
- Domain entities
- Business rules for Member, Wallet, Point Generator, Campaign, etc.

---

# Main components

## 1. `Result`

Represents the outcome of a command or use case that does not return data.

### Success example

```csharp
return Result.Success();
```

### Failure example

```csharp
return Result.Failure(
    Error.Conflict(
        "wallet.insufficient_balance",
        "Wallet balance is insufficient."));
```

### Validation failure example

```csharp
return Result.ValidationFailure([
    new ValidationError(
        "mobile",
        "Mobile number is invalid.",
        "member.mobile_invalid")
]);
```

---

## 2. `Result<T>`

Represents the outcome of a query or use case that returns data.

### Success example

```csharp
return Result<MemberDto>.Success(member);
```

### Failure example

```csharp
return Result<MemberDto>.Failure(
    Error.NotFound(
        "member.not_found",
        $"Member with id '{memberId}' was not found."));
```

---

## 3. `Error`

Represents an expected application error.

An error has:

- `Code`
- `Message`
- `Type`

### Example

```csharp
public static class MemberErrors
{
    public static Error NotFound(Guid memberId)
        => Error.NotFound(
            "member.not_found",
            $"Member with id '{memberId}' was not found.");
}
```

Business-specific errors must be defined inside each service, not inside this package.

---

## 4. `ErrorType`

Defines the semantic type of an error.

### Current values

```text
None
Validation
NotFound
Conflict
Unauthorized
Forbidden
Failure
Unexpected
```

`Application` only defines the meaning of the error. It does not decide the HTTP status code.

HTTP mapping belongs to `CustomerClub.BuildingBlocks.Api`.

### Example mapping in API layer

```text
ErrorType.Validation   -> 400 Bad Request
ErrorType.NotFound     -> 404 Not Found
ErrorType.Conflict     -> 409 Conflict
ErrorType.Unauthorized -> 401 Unauthorized
ErrorType.Forbidden    -> 403 Forbidden
ErrorType.Failure      -> 500 Internal Server Error
ErrorType.Unexpected   -> 500 Internal Server Error
```

---

## 5. `ValidationError`

Represents one validation error.

```csharp
new ValidationError(
    PropertyName: "mobile",
    ErrorMessage: "Mobile number is invalid.",
    ErrorCode: "member.mobile_invalid");
```

Multiple validation errors should be returned using:

```csharp
Result.ValidationFailure(errors);
```

or:

```csharp
Result<T>.ValidationFailure(errors);
```

---

# Request outcome flow

## Successful use case

```text
Application handler
  -> Result<T>.Success(value)
  -> API maps it to 200 OK
```

For commands without response body:

```text
Application handler
  -> Result.Success()
  -> API maps it to 204 No Content
```

---

## Expected business failure

```text
Application handler
  -> Result<T>.Failure(Error.NotFound(...))
  -> API maps it to 404 Not Found
```

### Example

```csharp
return Result<MemberDto>.Failure(
    Error.NotFound(
        "member.not_found",
        $"Member with id '{memberId}' was not found."));
```

---

## Validation failure

```text
Application handler
  -> Result<T>.ValidationFailure(errors)
  -> API maps it to 400 Bad Request
```

### Example

```csharp
return Result<MemberDto>.ValidationFailure([
    new ValidationError(
        "mobile",
        "Mobile number is invalid.",
        "member.mobile_invalid"),

    new ValidationError(
        "nationalCode",
        "National code is invalid.",
        "member.national_code_invalid")
]);
```

---

## Unexpected exception

Unexpected exceptions should not be converted to `Result`.

They should be allowed to bubble up and be handled by `CustomerClub.BuildingBlocks.Api`.

```text
Application handler
  -> throws unexpected exception
  -> API GlobalExceptionHandler
  -> 500 Internal Server Error
```

### Examples

- Database connection failure
- Null reference bug
- Unexpected infrastructure failure
- Invalid internal state

---

# Boundary with other Building Blocks

## Relationship with `CustomerClub.BuildingBlocks.Api`

`Application` creates `Result` and `Error`.

`Api` converts them to HTTP responses.

### Allowed dependency

```text
CustomerClub.BuildingBlocks.Api
  -> CustomerClub.BuildingBlocks.Application
```

### Not allowed

```text
CustomerClub.BuildingBlocks.Application
  -> CustomerClub.BuildingBlocks.Api
```

`Application` must never know about:

- `ControllerBase`
- `IActionResult`
- `IResult`
- `ProblemDetails`
- HTTP status codes

---

## Relationship with `CustomerClub.BuildingBlocks.Contracts`

`Contracts` is for cross-service contracts such as integration events and shared message metadata.

`Application` is for use-case results inside a service.

```text
Result/Error         -> Application
IntegrationEvent     -> Contracts
Event metadata       -> Contracts
```

---

## Relationship with `CustomerClub.BuildingBlocks.Persistence`

`Persistence` owns database and transactional concerns.

`Application` must not reference persistence implementations.

Avoid putting these in this package:

- EF Core
- `DbContext`
- Repository implementations
- Transaction implementations
- Outbox persistence

---

## Relationship with `CustomerClub.BuildingBlocks.Messaging`

`Messaging` owns publish/consume abstractions and broker integration.

`Application` should not publish messages directly through this package.

Messaging behavior belongs to the service composition layer and infrastructure, not to this core application chassis.

---

# Expected usage in a microservice

## Example handler

```csharp
public sealed class GetMemberByIdHandler
{
    public async Task<Result<MemberDto>> Handle(
        GetMemberByIdQuery query,
        CancellationToken cancellationToken)
    {
        var member = await repository.GetByIdAsync(
            query.MemberId,
            cancellationToken);

        if (member is null)
        {
            return Result<MemberDto>.Failure(
                MemberErrors.NotFound(query.MemberId));
        }

        return Result<MemberDto>.Success(member);
    }
}
```

## Controller usage

Controller usage happens through `CustomerClub.BuildingBlocks.Api`:

```csharp
var result = await handler.Handle(query, cancellationToken);

return result.ToActionResult(this);
```

## Minimal API usage

Minimal API usage also happens through `CustomerClub.BuildingBlocks.Api`:

```csharp
var result = await handler.Handle(query, cancellationToken);

return result.ToHttpResult(httpContext);
```

---

# Design rules

## Rule 1: Expected business failures should return `Result`

### Good

```csharp
return Result.Failure(
    Error.Conflict(
        "wallet.insufficient_balance",
        "Wallet balance is insufficient."));
```

### Avoid

```csharp
throw new Exception("Wallet balance is insufficient.");
```

---

## Rule 2: Exceptions are for unexpected failures

Use exceptions for unexpected technical failures only.

Expected business outcomes should be represented with `Result/Error`.

---

## Rule 3: Keep this package framework-neutral

This package should stay independent from:

- ASP.NET Core
- EF Core
- RabbitMQ/Kafka
- MassTransit
- Dapr
- Swagger
- `ProblemDetails`

If a feature needs one of these, it probably belongs in another Building Block.

---

## Rule 4: Business-specific errors stay inside services

Do not add these to this package:

```text
member.not_found
wallet.insufficient_balance
point.rule_not_matched
campaign.expired
```

Instead, define them inside the owning service:

```csharp
public static class WalletErrors
{
    public static Error InsufficientBalance()
        => Error.Conflict(
            "wallet.insufficient_balance",
            "Wallet balance is insufficient.");
}
```

---

## Rule 5: Do not use `ErrorType` as business logic

`ErrorType` is only a semantic category.

### Good

```csharp
Error.NotFound(
    "member.not_found",
    "Member was not found.")
```

### Avoid

```csharp
ErrorType.NotFound
```

Every service-specific error must have a meaningful `Code`.

---

# Future boundaries

This package may later include:

- CQRS marker interfaces
- Command/query handler abstractions
- Date/time provider abstractions
- GUID provider abstraction
- Validation abstractions
- Optional pipeline contracts

This package should not include:

- MediatR behaviors directly unless intentionally approved
- FluentValidation-specific code unless separated into another package
- HTTP mapping
- Database transaction behavior
- Outbox behavior
- Messaging behavior
- Domain entities
- Service-specific use cases

### Recommended separation

If MediatR support is needed, prefer a separate package:

```text
CustomerClub.BuildingBlocks.Application.MediatR
```

If FluentValidation support is needed, prefer a separate package:

```text
CustomerClub.BuildingBlocks.Application.Validation
```

---

# Recommended file structure

```text
CustomerClub.BuildingBlocks.Application
│
├── Results
│   ├── Error.cs
│   ├── ErrorType.cs
│   ├── Result.cs
│   ├── ResultOfT.cs
│   └── ValidationError.cs
│
└── README.md
```

Current implementation may keep `Result` and `Result<T>` in the same file, but separating `Result<T>` into `ResultOfT.cs` is recommended for readability.

---

# Summary

`CustomerClub.BuildingBlocks.Application` is the shared application result model for Customer Club microservices.

It standardizes how use cases return:

- Success
- Failure
- Validation failure
- Typed success values

It must remain small, stable, and framework-neutral.

```text
Application -> models the outcome
Api         -> translates the outcome to HTTP
```

Services own their business-specific errors and rules.