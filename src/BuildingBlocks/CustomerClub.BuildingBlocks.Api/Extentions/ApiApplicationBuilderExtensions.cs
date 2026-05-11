using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace CustomerClub.BuildingBlocks.Api.Extentions;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseCustomerClubApiConventions(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}