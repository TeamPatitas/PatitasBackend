using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;

public interface IPetService
{
    // Task<PetResponse> CreatePetAsync(PetRequest request);
    // Task<PetResponse> UpdatePetAsync(Guid petId, PetRequest request);
    // Task<bool> DeletePetAsync(Guid petId);
    Task<PetResponse?> GetPetByIdAsync(Guid petId);
    //Task<IEnumerable<PetResponse>> GetAllPetsAsync();
}