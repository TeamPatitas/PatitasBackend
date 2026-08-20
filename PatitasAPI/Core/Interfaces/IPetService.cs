using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Utils;

namespace PatitasAPI.Core.Interfaces;

public interface IPetService
{
    Task<CreatePetResponse> CreatePetAsync(CreatePetRequest request, string userId);
    Task<CreatePetResponse?> GetPetByIdAsync(Guid petId, string? requesterUserId = null);
    Task<PagedResponse<CreatePetResponse>> GetAllPetsAsync(int page, int pageSize, string? requesterUserId = null);
    Task<CreatePetResponse?> UpdatePetAsync(Guid petId, UpdatePetRequest request, string userId);
    Task<bool> DeletePetAsync(Guid petId, string userId);
}