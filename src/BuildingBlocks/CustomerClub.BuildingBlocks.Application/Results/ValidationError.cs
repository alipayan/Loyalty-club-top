namespace CustomerClub.BuildingBlocks.Application.Results;

public sealed record ValidationError(
    string PropertyName,
    string ErrorMessage,
    string? ErrorCode = null);