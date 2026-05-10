namespace CustomerClub.BuildingBlocks.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class CustomerClubControllerBase : ControllerBase
{
    protected string TraceId => HttpContext.TraceIdentifier;

    protected string? CorrelationId =>
        HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var value)
            ? value.ToString()
            : null;
}