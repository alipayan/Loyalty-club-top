namespace CustomerClub.BuildingBlocks.Api.ProblemDetails;

public static class ProblemDetailsExtensions
{
    extension(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)
    {
        public Microsoft.AspNetCore.Mvc.ProblemDetails WithStandardExtensions(
        HttpContext httpContext,
        string? serviceName = null,
        string? errorCode = null)
        {
            problemDetails.Extensions[ProblemDetailsDefaults.TraceIdKey] = httpContext.TraceIdentifier;

            if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
                problemDetails.Extensions[ProblemDetailsDefaults.CorrelationIdKey] = correlationId.ToString();

            if (!string.IsNullOrWhiteSpace(serviceName))
                problemDetails.Extensions[ProblemDetailsDefaults.ServiceNameKey] = serviceName;

            if (!string.IsNullOrWhiteSpace(errorCode))
                problemDetails.Extensions[ProblemDetailsDefaults.ErrorCodeKey] = errorCode;

            return problemDetails;
        }
    }
}