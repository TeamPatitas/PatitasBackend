using Microsoft.AspNetCore.HttpOverrides;
using PatitasAPI.API;
using PatitasAPI.Infraestructure;
using PatitasAPI.Infraestructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddInfraestructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
}

app.MapAllEndpoints();
app.MapGet("/", () => "Hola pez, PatitasAPI made with ❤️ by GM4 & PatitasTeam");
app.UseAuthorization();
app.MapControllers();
app.Run();
