using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Core.Utils;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure.Auth;

public class IdentityAuthService(UserManager<AppUser> userManager, IStorageService storageService, IEmailService emailService, PatitasDbContext dbContext) : IAuthService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IStorageService _storageService = storageService;
    private readonly IEmailService _emailService = emailService;
    private readonly PatitasDbContext _dbContext = dbContext;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new Exception("Credenciales inválidas");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new Exception("Credenciales inválidas");

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Token = token,
            Roles = [.. roles]
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null) throw new Exception("Ese correo ya está en uso");
        if (!Enum.IsDefined(typeof(Gender), request.Gender))
        {
            throw new Exception("Valor de Género inválido. Use 0 para MASCULINO o 1 para FEMENINO.");

        }

        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            BirthDate = request.BirthDate,
            Gender = (Gender)request.Gender
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new Exception("Error al crear el usuario: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        await _userManager.AddToRoleAsync(user, "User");

        if (request.Photo != null)
        {
            if (request.Photo.Length > 10 * 1024 * 1024) throw new Exception("Foto excede 10MB");
            var key = $"users/{user.Id}.webp";
            var url = await _storageService.UploadFileAsync(request.Photo, key);
            user.PhotoUrl = url;
            await _userManager.UpdateAsync(user);
        }

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);

        await SendEmailVerificationAsync(Guid.Parse(user.Id));

        return new AuthResponse
        {
            Token = token,
            Roles = [.. roles]
        };
    }

    public async Task<UserResponse> GetUserAsync(Guid userId)
    {
        var appUser = await _dbContext.Users.Include(u => u.Shelter).FirstOrDefaultAsync(u => u.Id == userId.ToString());
        if (appUser == null) throw new Exception("Usuario no encontrado");
        var roles = await _userManager.GetRolesAsync(appUser);
        var role = roles.FirstOrDefault() ?? "User";
        return new UserResponse
        {
            Id = appUser.Id,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName,
            Email = appUser.Email ?? "",
            IsEmailConfirmed = appUser.EmailConfirmed,
            Gender = (int)appUser.Gender,
            PhotoUrl = appUser.PhotoUrl ?? "",
            BirthDate = appUser.BirthDate,
            Role = role,
            ShelterId = appUser.ShelterId
        };
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser == null) throw new Exception("Usuario no encontrado");

        if (request.FirstName != null)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName)) throw new Exception("FirstName no puede estar vacío");
            appUser.FirstName = request.FirstName;
        }
        if (request.LastName != null)
        {
            if (string.IsNullOrWhiteSpace(request.LastName)) throw new Exception("LastName no puede estar vacío");
            appUser.LastName = request.LastName;
        }
        if (request.BirthDate.HasValue) appUser.BirthDate = request.BirthDate.Value;
        if (request.Gender.HasValue)
        {
            if (!Enum.IsDefined(typeof(Gender), request.Gender.Value))
                throw new Exception("Valor de Género inválido. Use 0 para MASCULINO o 1 para FEMENINO.");
            appUser.Gender = (Gender)request.Gender.Value;
        }

        if (request.Photo != null)
        {
            var photo = request.Photo;
            if (photo.Length == 0) throw new Exception("Archivo vacío.");
            if (photo.Length > 10 * 1024 * 1024) throw new Exception("Archivo excede 10MB.");
            var ct = photo.ContentType.ToLowerInvariant();
            if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
                throw new Exception($"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp.");
            var key = $"users/{appUser.Id}.webp";
            var url = await _storageService.UploadFileAsync(photo, key);
            appUser.PhotoUrl = url;
        }

        var result = await _userManager.UpdateAsync(appUser);
        if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        var roles = await _userManager.GetRolesAsync(appUser);
        var role = roles.FirstOrDefault() ?? "User";
        return new UserResponse
        {
            Id = appUser.Id,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName,
            Email = appUser.Email ?? "",
            IsEmailConfirmed = appUser.EmailConfirmed,
            Gender = (int)appUser.Gender,
            PhotoUrl = appUser.PhotoUrl ?? "",
            BirthDate = appUser.BirthDate,
            Role = role,
            ShelterId = appUser.ShelterId
        };
    }

    public async Task SendEmailVerificationAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new Exception("Usuario no encontrado");
        
        if (user.EmailConfirmed) throw new Exception("El correo ya ha sido verificado");

        var emailRawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedTokenBytes = Encoding.UTF8.GetBytes(emailRawToken);
        var safeToken = WebEncoders.Base64UrlEncode(encodedTokenBytes);
        var api_url = PatitasEnv.GetEnvVariable("API_BASE_URL");
        var verificationLink = $"{api_url}/auth/verify-email?userId={user.Id}&token={safeToken}";

        if(PatitasEnv.IsDev()) {
            Console.WriteLine("\n=======================================================");
            Console.WriteLine($"📧 NUEVO REGISTRO: {user.Email}");
            Console.WriteLine($"🔗 LINK DE VERIFICACIÓN:");
            Console.WriteLine(verificationLink);
            Console.WriteLine("=======================================================\n");
        } else {
            string[] images = [
                "https://i.pinimg.com/736x/eb/0b/19/eb0b19a194ac9c38f5245c8f4de14ef8.jpg",
                "https://i.pinimg.com/736x/90/87/94/908794de8979891aac4e0db92e4a4a94.jpg",
                "https://i.pinimg.com/736x/1c/f7/09/1cf70991823f65623bd192ea7dbc813a.jpg",
                "https://i.pinimg.com/736x/8f/a9/e5/8fa9e5031d7e8bac8b410993278e21f7.jpg",
                "https://i.pinimg.com/736x/18/d5/c6/18d5c64d2bfb606540294f5c1e57b20d.jpg",
                "https://i.pinimg.com/736x/1d/56/23/1d5623374310648a333757b264c43623.jpg",
                "https://i.pinimg.com/736x/bb/f9/de/bbf9de946d694708b9140ea5fc278bfa.jpg",
                "https://i.pinimg.com/1200x/6b/3e/27/6b3e2732f2d45ee33e45f5349051232c.jpg"
            ];

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #4CAF50;'>¡Bienvenido a Patitas al Rescate! 🐾</h2>
                    <p>Hola,</p>
                    <p>Gracias por unirte a nuestra plataforma. Para poder iniciar sesión y empezar a adoptar a los perritos, necesitamos verificar tu correo.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                            Verificar mi cuenta
                        </a>
                    </div>
                    <div style='text-align: center; margin: 30px 0;'>
                        <img style='width: 200px; border-radius: 20px;' src='{images[new Random().Next(images.Length)]}' alt='Imagen de bienvenida' />
                    </div>
                    <p style='color: #777; font-size: 12px;'>Si el botón no funciona, copia y pega este enlace en tu navegador:<br>{verificationLink}</p>
                </div>
            ";

            await _emailService.SendEmailAsync(
                email: user.Email!,
                subject: "Verifica tu cuenta - Patitas al Rescate",
                htmlBody: emailBody
            );
        }
    }

    public async Task VerifyEmailAsync(Guid userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new Exception("Usuario no encontrado");
        var decodedTokenBytes = WebEncoders.Base64UrlDecode(token);
        var originalToken = Encoding.UTF8.GetString(decodedTokenBytes);
        var result = await _userManager.ConfirmEmailAsync(user, originalToken);
        if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<bool> DeleteUserAsync(Guid targetUserId, Guid requesterUserId)
    {
        var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
        if (targetUser == null) return false;

        var requesterUser = await _userManager.FindByIdAsync(requesterUserId.ToString());
        if (requesterUser == null) throw new UnauthorizedAccessException("Solicitante no encontrado");

        var isDev = await _userManager.IsInRoleAsync(requesterUser, "Dev");
        var isSelf = targetUserId == requesterUserId;
        if (!isSelf && !isDev)
            throw new UnauthorizedAccessException("Solo puedes borrar tu propia cuenta o ser Dev");

        if (targetUser.ShelterId != null)
            throw new InvalidOperationException("No se puede borrar usuario con refugio asignado");

        var pendingAdoptions = await _dbContext.Adoptions
            .Where(a => a.AppUserId == targetUser.Id && a.Status == AdoptionStatus.REQUESTED)
            .ToListAsync();
        foreach (var ad in pendingAdoptions)
        {
            ad.Status = AdoptionStatus.CANCELLED;
        }
        if (pendingAdoptions.Count > 0)
            await _dbContext.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(targetUser);
        if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return true;
    }

    // Private
    private async Task<string> GenerateJwt(AppUser user)
    {
        var jwtKey = PatitasEnv.GetEnvVariable("JWT_SECRET_KEY");
        var roles = await _userManager.GetRolesAsync(user);
        var userRole = roles.FirstOrDefault() ?? "Worker";

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (JwtRegisteredClaimNames.Email, user.Email!),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.Role, userRole)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(20),
            Issuer = "PatitasTest",
            Audience = "PatitasTest",
            SigningCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
