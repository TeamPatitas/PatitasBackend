using PatitasAPI.API.Endpoints;
namespace PatitasAPI.API;

public static class IndexEndpoints
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapPetEndpoints();
        app.MapShelterEndpoints();

        if(app.Environment.IsDevelopment())
        {
            app.MapDevEndpoints();
        }
    }
}