namespace CustomerClub.BuildingBlocks.ServiceDefaults.Configuration;

public sealed class ServiceDefaultsOptions
{
    public string ServiceName { get; set; } = "unknown-service";

    public bool EnableHealthChecks { get; set; } = true;

    public bool EnableCorrelation { get; set; } = true;

    public bool EnableJsonDefaults { get; set; } = true;

    public bool EnableHttpContextAccessor { get; set; } = true;
}