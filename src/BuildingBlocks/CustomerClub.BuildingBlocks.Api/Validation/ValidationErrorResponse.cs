namespace CustomerClub.BuildingBlocks.Api.Validation;

public sealed record ValidationErrorResponse(
    string Field,
    string Message,
    string? Code = null);
