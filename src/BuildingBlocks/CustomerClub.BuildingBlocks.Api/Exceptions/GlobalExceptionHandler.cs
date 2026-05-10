using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace CustomerClub.BuildingBlocks.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IOptions<CustomerClubApiOptions> options) : IExceptionHandler
{
    private readonly CustomerClubApiOptions _options = options.Value;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ApiException apiException => CreateApiProblemDetails(httpContext, apiException),

            _ => CreateUnexpectedProblemDetails(httpContext, exception)
        };

        logger.LogError(
            exception,
            "Unhandled exception occurred. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}",
            problemDetails.Status,
            problemDetails.Extensions.TryGetValue("errorCode", out var errorCode) ? errorCode : null);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateApiProblemDetails(
        HttpContext httpContext,
        ApiException exception)
    {
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = exception.StatusCode,
            Title = ReasonPhrases.GetReasonPhrase(exception.StatusCode),
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception.Details is not null)
            problemDetails.Extensions["details"] = exception.Details;

        return problemDetails.WithStandardExtensions(
            httpContext,
            _options.ServiceName,
            exception.ErrorCode);
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails CreateUnexpectedProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        var detail = _options.IncludeExceptionDetails
            ? exception.ToString()
            : "An unexpected error occurred.";

        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = detail,
            Instance = httpContext.Request.Path
        }.WithStandardExtensions(
            httpContext,
            _options.ServiceName,
            "internal.unexpected_error");
    }
}