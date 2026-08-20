using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IPetService
{
    Task<PetResponse> CreatePetAsync(CreatePetRequest request, string userId);
    Task<PetResponse?> GetPetByIdAsync(Guid petId, string? requesterUserId = null);
    Task<PagedResponse<PetResponse>> GetAllPetsAsync(int page, int pageSize, string? requesterUserId = null);
    Task<PetResponse?> UpdatePetAsync(Guid petId, UpdatePetRequest request, string userId);
    Task<bool> DeletePetAsync(Guid petId, string userId);
}