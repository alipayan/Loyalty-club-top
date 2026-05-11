namespace CustomerClub.BuildingBlocks.Api.Exceptions.CommonExceptions;

public sealed class NotFoundException(string message, string errorCode = "resource.not_found")
    : ApiException(message, StatusCodes.Status404NotFound, errorCode);