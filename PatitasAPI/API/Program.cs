using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using PatitasAPI.API;
using PatitasAPI.Infraestructure;
using PatitasAPI.Infraestructure.Data;
using PatitasAPI.Infraestructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
PatitasEnv.ValidateEnv();
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PatitasAlRescate API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "xd hola."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try {
        var ctx = services.GetRequiredService<PatitasDbContext>();
        ctx.Database.EnsureCreated();
    } catch(Exception ex) {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al crear la base de datos.");
    }

    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
}

app.MapAllEndpoints();
app.MapGet("/", () => "Hola pez, PatitasAPI made with ❤️ by GM4 & PatitasTeam");
app.UseAuthorization();
app.MapControllers();
app.Run();
