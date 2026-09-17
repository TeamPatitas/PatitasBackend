using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Core.Utils;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure.Auth;

public class IdentityAuthService(UserManager<AppUser> userManager, IStorageService storageService, PatitasDbContext dbContext) : IAuthService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IStorageService _storageService = storageService;
    private readonly PatitasDbContext _dbContext = dbContext;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new Exception("Credenciales inválidas");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new Exception("Credenciales inválidas");

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse(token, [.. roles]);
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

        return new AuthResponse(token, [.. roles]);
    }

    public async Task<UserResponse> GetUserAsync(Guid userId)
    {
        var appUser = await _dbContext.Users.Include(u => u.Shelter).FirstOrDefaultAsync(u => u.Id == userId.ToString()) ?? throw new Exception("Usuario no encontrado");
        var roles = await _userManager.GetRolesAsync(appUser);
        var role = roles.FirstOrDefault() ?? "User";
        return new UserResponse(
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
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var appUser = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new Exception("Usuario no encontrado");
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
        return new UserResponse(
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
    }

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