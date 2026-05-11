using CustomerClub.BuildingBlocks.Observability;
using Microsoft.AspNetCore.Http;

namespace CustomerClub.BuildingBlocks.ServiceDefaults.Correlation;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(ObservabilityConventions.CorrelationHeader))
        {
            context.Request.Headers.Append(
                ObservabilityConventions.CorrelationHeader,
                context.TraceIdentifier);
        }

        context.Response.Headers.TryAdd(
            ObservabilityConventions.CorrelationHeader,
            context.Request.Headers[ObservabilityConventions.CorrelationHeader]);

        await next(context);
    }
}