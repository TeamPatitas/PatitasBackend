namespace PatitasAPI.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string key);
    Task<string> UploadPetPhotoAsync(IFormFile file, Guid petId, int photoIndex);
}