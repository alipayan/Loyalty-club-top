namespace CustomerClub.BuildingBlocks.Api.Exceptions.CommonExceptions;

public sealed class ConflictException(string message, string errorCode = "resource.conflict")
    : ApiException(message, StatusCodes.Status409Conflict, errorCode);