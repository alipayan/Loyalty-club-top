var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerClubServiceDefaults("PointGenerator");
builder.Services.AddCustomerClubApiConventions();
builder.Services.AddAuthorization();
builder.Services.AddPointGeneratorApplication();

var app = builder.Build();
app.UseCustomerClubDefaultPipeline();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();

app.MapGet("/api/sample/health-context", () => Results.Ok(new
{
    service = "PointGenerator",
    utcNow = DateTimeOffset.UtcNow
}));

app.Run();
