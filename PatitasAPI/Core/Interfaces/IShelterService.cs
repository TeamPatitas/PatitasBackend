using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IShelterService
{
    Task<CreateShelterResponse> CreateShelterAsync(CreateShelterRequest request);
//     Task<ShelterResponse?> GetShelterByIdAsync(Guid shelterId);
//     Task<PagedResponse<ShelterResponse>> GetAllSheltersAsync(int page, int pageSize);
//     Task<ShelterResponse?> UpdateShelterAsync(Guid shelterId, UpdateShelterRequest request, string userId);
//     Task<bool> DeleteShelterAsync(Guid shelterId, string userId);
}