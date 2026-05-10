namespace CustomerClub.BuildingBlocks.Api.Exceptions.CommonExceptions;

public sealed class BadRequestException(string message, string errorCode = "request.bad_request")
    : ApiException(message, StatusCodes.Status400BadRequest, errorCode);