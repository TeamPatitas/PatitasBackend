using System.Security.Claims;
namespace PatitasAPI.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // GET /user en dominio base (fuera de /auth)
        app.MapGet("/user", async (ClaimsPrincipal user, UserManager<AppUser> userManager, PatitasDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var appUser = await db.Users.Include(u => u.Shelter).FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null) return Results.NotFound(new { message = "Usuario no encontrado" });
            var roles = await userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault() ?? "User";
            var response = new UserResponse(
                appUser.Id,
                appUser.FirstName,
                appUser.LastName,
                appUser.Email ?? "",
                appUser.EmailConfirmed,
                (int)appUser.Gender,
                appUser.PhotoUrl ?? "",
                appUser.BirthDate,
                role,
                appUser.ShelterId
            );
            return Results.Ok(response);
        }).WithName("GetCurrentUser").RequireAuthorization("User").WithTags("Authentication");

        // PATCH /user
        app.MapPatch("/user", async ([FromForm] UpdateUserRequest request, ClaimsPrincipal user, UserManager<AppUser> userManager, IStorageService storageService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            var appUser = await userManager.FindByIdAsync(userId);
            if (appUser == null) return Results.NotFound(new { message = "Usuario no encontrado" });

            if (request.FirstName != null)
            {
                if (string.IsNullOrWhiteSpace(request.FirstName)) return Results.BadRequest(new { message = "FirstName no puede estar vacío" });
                appUser.FirstName = request.FirstName;
            }
            if (request.LastName != null)
            {
                if (string.IsNullOrWhiteSpace(request.LastName)) return Results.BadRequest(new { message = "LastName no puede estar vacío" });
                appUser.LastName = request.LastName;
            }
            if (request.BirthDate.HasValue) appUser.BirthDate = request.BirthDate.Value;
            if (request.Gender.HasValue)
            {
                if (!Enum.IsDefined(typeof(PatitasAPI.Core.Utils.Gender), request.Gender.Value))
                    return Results.BadRequest(new { message = "Valor de Género inválido. Use 0 para MASCULINO o 1 para FEMENINO." });
                appUser.Gender = (PatitasAPI.Core.Utils.Gender)request.Gender.Value;
            }

            if (request.Photo != null)
            {
                var photo = request.Photo;
                if (photo.Length == 0) return Results.BadRequest(new { message = "Archivo vacío." });
                if (photo.Length > 10 * 1024 * 1024) return Results.BadRequest(new { message = "Archivo excede 10MB." });
                var ct = photo.ContentType.ToLowerInvariant();
                if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
                    return Results.BadRequest(new { message = $"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp." });

                var key = $"users/{appUser.Id}.webp";
                var url = await storageService.UploadFileAsync(photo, key);
                appUser.PhotoUrl = url;
            }

            var result = await userManager.UpdateAsync(appUser);
            if (!result.Succeeded) return Results.BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            var roles = await userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault() ?? "User";
            var response = new UserResponse(
                appUser.Id,
                appUser.FirstName,
                appUser.LastName,
                appUser.Email ?? "",
                appUser.EmailConfirmed,
                (int)appUser.Gender,
                appUser.PhotoUrl ?? "",
                appUser.BirthDate,
                role,
                appUser.ShelterId
            );
            return Results.Ok(response);
        }).WithName("UpdateCurrentUser").RequireAuthorization("User").WithTags("Authentication").DisableAntiforgery();

        var group = app.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            try
            {
                var response = await authService.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Iniciar Sesión");

        group.MapPost("/register", async ([FromForm] RegisterRequest request, IAuthService authService) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("Registrarse").DisableAntiforgery();
    }
}