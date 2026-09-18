using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IHealthService
{
    Task<HealthResponse> CheckHealthAsync();
}
