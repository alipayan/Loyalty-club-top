using CustomerClub.BuildingBlocks.Application.Results;

namespace CustomerClub.BuildingBlocks.Api.Results;

public static class ResultHttpExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return CreateProblemResult(result, controller);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);

        return CreateProblemResult(result, controller);
    }

    public static IResult ToHttpResult(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return CreateProblemHttpResult(result, httpContext);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return CreateProblemHttpResult(result, httpContext);
    }

    private static IActionResult CreateProblemResult(Result result, ControllerBase controller)
    {
        var statusCode = MapStatusCode(result.Error.Type);

        var problemDetails = CreateProblemDetails(
            result,
            controller.HttpContext,
            statusCode);

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static IResult CreateProblemHttpResult(Result result, HttpContext httpContext)
    {
        var statusCode = MapStatusCode(result.Error.Type);

        var problemDetails = CreateProblemDetails(
            result,
            httpContext,
            statusCode);

        return TypedResults.Problem(problemDetails);
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        Result result,
        HttpContext httpContext,
        int statusCode)
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(result.Error.Type),
            Detail = result.Error.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.WithStandardExtensions(
            httpContext,
            errorCode: result.Error.Code);

        if (result.ValidationErrors.Count > 0)
        {
            problemDetails.Extensions["errors"] =
                result.ValidationErrors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage,
                    code = error.ErrorCode
                }).ToArray();
        }

        return problemDetails;
    }

    private static int MapStatusCode(ErrorType errorType)
        => errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetTitle(ErrorType errorType)
        => errorType switch
        {
            ErrorType.Validation => "Validation Failed",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Failure => "Failure",
            ErrorType.Unexpected => "Unexpected Error",
            _ => "Error"
        };
}