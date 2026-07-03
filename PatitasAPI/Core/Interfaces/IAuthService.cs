using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
}