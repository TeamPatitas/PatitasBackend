using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PatitasAPI.API;
using PatitasAPI.Infraestructure;
using PatitasAPI.Infraestructure.Data;
using PatitasAPI.Infraestructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

PatitasEnv.ValidateEnv();
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors();
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});;

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

    c.OperationFilter<DocsFilter>();
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    
    c.IncludeXmlComments(xmlPath);
});


var app = builder.Build();
var baseUrl = PatitasEnv.GetEnvVariable("API_BASE_URL");
if(PatitasEnv.HasFrontendUrl()){
    var frontendUrl = PatitasEnv.GetEnvVariable("FRONTEND_URL");
    app.UseCors(policy => policy.WithOrigins(frontendUrl).AllowAnyHeader().AllowAnyMethod());
}
app.UseSwagger();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patitas API v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try {
        var ctx = services.GetRequiredService<PatitasDbContext>();
        ctx.Database.EnsureCreated();
    } catch(Exception ex) {
        logger.LogError(ex, "Ocurrió un error al crear la base de datos.");
    }

    await AuthSeeder.SeedRolesAsync(scope.ServiceProvider);

    try {
        var dbContext = scope.ServiceProvider.GetRequiredService<PatitasDbContext>();
        logger.BeginScope("Aplicando migraciones de la base de datos...");
        await dbContext.Database.MigrateAsync();
    } catch (Exception ex) {
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones de la base de datos.");
    }
} 

app.MapAllEndpoints();
app.MapGet("/", () => "Hola pez, PatitasAPI made with ❤️ by GM4 & PatitasTeam");
app.UseAuthorization();
app.MapControllers();

if(app.Environment.IsDevelopment()) app.Logger.LogInformation($"Swagger UI: {baseUrl}/swagger");
app.Logger.LogInformation($"Patitas API en linea :D {baseUrl}/");

app.Run();
