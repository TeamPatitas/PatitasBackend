using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Auth;

public class IdentityAuthService(UserManager<AppUser> userManager, IConfiguration configuration) : IAuthService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new Exception("Invalid credentials");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid) throw new Exception("Invalid credentials");

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse(token, [.. roles]);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null) throw new Exception("Email already in use");
        if (!Enum.IsDefined(typeof(Gender), request.Gender))
        {
            throw new Exception("Invalid gender value. Use 0 for MALE or 1 for FEMALE.");
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
        if (!result.Succeeded) throw new Exception("Error creating user");
        await _userManager.AddToRoleAsync(user, "User");

        var token = await GenerateJwt(user);
        var roles = await _userManager.GetRolesAsync(user);
        
        return new AuthResponse(token, [.. roles]);
    }

    private async Task<string> GenerateJwt(AppUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
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
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}