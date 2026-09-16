using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IShelterService
{
    Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, string userId);
    Task<ShelterResponse?> EnableAsync(Guid shelterId);
    Task<ShelterResponse?> DisableAsync(Guid shelterId);
    Task<PagedResponse<ShelterResponse>> GetAllAsync(int page, int pageSize, string? requesterUserId = null);
    Task<ShelterResponse?> GetByIdAsync(Guid shelterId, string? requesterUserId = null);
    Task<ShelterResponse?> UpdateAsync(Guid shelterId, UpdateShelterRequest request, string userId);
}