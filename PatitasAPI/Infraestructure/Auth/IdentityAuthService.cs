using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Core.Utils;

namespace PatitasAPI.Infraestructure.Auth;

public class IdentityAuthService(UserManager<AppUser> userManager) : IAuthService
{
    private readonly UserManager<AppUser> _userManager = userManager;

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

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse(token, [.. roles]);
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