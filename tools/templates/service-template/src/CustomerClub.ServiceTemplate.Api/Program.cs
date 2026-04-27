using CustomerClub.BuildingBlocks.Api;
using CustomerClub.BuildingBlocks.ServiceDefaults;
using CustomerClub.ServiceTemplate.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerClubServiceDefaults("ServiceTemplate");
builder.Services.AddCustomerClubApiConventions();
builder.Services.AddAuthorization();
builder.Services.AddServiceTemplateApplication();

var app = builder.Build();
app.UseCustomerClubDefaultPipeline();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();

app.MapGet("/api/sample/health-context", () => Results.Ok(new
{
    service = "ServiceTemplate",
    utcNow = DateTimeOffset.UtcNow
}));

app.MapPost("/api/sample/commands/create", (CreateSampleCommand command, SampleCommandHandler handler, CancellationToken ct)
    => handler.HandleAsync(command, ct));

app.Run();
