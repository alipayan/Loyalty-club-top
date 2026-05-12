namespace CustomerClub.BuildingBlocks.Api.Extentions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerClubApiConventions(
        this IServiceCollection services,
        Action<CustomerClubApiOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
            services.Configure(configureOptions);
        else
            services.Configure<CustomerClubApiOptions>(_ => { });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var apiOptions = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<CustomerClubApiOptions>>()
                    .Value;

                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(error =>
                        new ValidationErrorResponse(
                            Field: x.Key,
                            Message: error.ErrorMessage)))
                    .ToArray();

                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Instance = context.HttpContext.Request.Path
                };

                problemDetails.Extensions["errors"] = errors;

                problemDetails.WithStandardExtensions(
                    context.HttpContext,
                    apiOptions.ServiceName,
                    "validation.failed");

                return new BadRequestObjectResult(problemDetails);
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddCustomerClubApiVersioning();

        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        });

        return services;
    }
}