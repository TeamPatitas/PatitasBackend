using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IShelterService
{
    Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest request, string userId);
    Task<ShelterResponse?> EnableAsync(Guid shelterId);
    Task<ShelterResponse?> DisableAsync(Guid shelterId);
    Task<PagedResponse<ShelterResponse>> GetAllAsync(int page, int pageSize, string? requesterUserId = null);
    Task<ShelterResponse?> GetByIdAsync(Guid shelterId, string? requesterUserId = null);
//     Task<ShelterResponse?> GetShelterByIdAsync(Guid shelterId);
//     Task<PagedResponse<ShelterResponse>> GetAllSheltersAsync(int page, int pageSize);
//     Task<ShelterResponse?> UpdateShelterAsync(Guid shelterId, UpdateShelterRequest request, string userId);
//     Task<bool> DeleteShelterAsync(Guid shelterId, string userId);
}