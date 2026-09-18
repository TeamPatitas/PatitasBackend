using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.API.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", async (IHealthService healthService) =>
        {
            var result = await healthService.CheckHealthAsync();
            return result.Status == "UP" ? Results.Ok(result) : Results.Json(result, statusCode: 503);
        }).WithName("HealthCheck").RequireAuthorization("DevOnly").WithTags("Health");
    }
}
