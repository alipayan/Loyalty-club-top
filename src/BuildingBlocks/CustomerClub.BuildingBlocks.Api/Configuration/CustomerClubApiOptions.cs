namespace CustomerClub.BuildingBlocks.Api.Configuration;

public sealed class CustomerClubApiOptions
{
    public string ServiceName { get; set; } = "unknown-service";

    public string ApiTitle { get; set; } = "Customer Club API";

    public bool EnableSwagger { get; set; } = true;

    public bool EnableApiVersioning { get; set; } = true;

    public bool IncludeExceptionDetails { get; set; } = false;
}
