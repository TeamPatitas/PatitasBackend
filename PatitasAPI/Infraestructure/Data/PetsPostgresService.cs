using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure.Data;

public class PetsPostgresService(PatitasDbContext dbContext, UserManager<AppUser> userManager, IStorageService storageService) : IPetService
{
    private readonly PatitasDbContext _dbContext = dbContext;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IStorageService _storageService = storageService;

    private static PetResponse ToResponse(Pet pet) => new(
        pet.Id,
        pet.Name,
        pet.Species,
        pet.Breed,
        pet.Gender,
        pet.Temperament,
        pet.Story,
        pet.Photos,
        pet.Available,
        pet.ShelterId
    );

    private static void ValidatePhotoFiles(List<IFormFile>? photos)
    {
        if (photos == null) return;
        if (photos.Count > 3)
            throw new ArgumentException("Máximo 3 imágenes por mascota.");
        foreach (var file in photos)
        {
            if (file.Length == 0) throw new ArgumentException("Archivo vacío.");
            if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("Archivo excede 10MB.");
            var ct = file.ContentType.ToLowerInvariant();
            if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
                throw new ArgumentException($"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp.");
        }
    }

    private async Task<AppUser> GetUserOrThrowAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new UnauthorizedAccessException("Usuario no encontrado.");
        return user;
    }

    private async Task<bool> IsShelterOwnerOrDevAsync(AppUser user)
    {
        return await _userManager.IsInRoleAsync(user, "ShelterOwner") || await _userManager.IsInRoleAsync(user, "Dev");
    }

    public async Task<PetResponse> CreatePetAsync(CreatePetRequest request, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null) throw new InvalidOperationException("No existe Shelter asociado a tu usuario.");

        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name es requerido.");
        if (request.Name.Length > 100) throw new ArgumentException("Name máximo 100 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Breed)) throw new ArgumentException("Breed es requerido.");
        if (string.IsNullOrWhiteSpace(request.Temperament)) throw new ArgumentException("Temperament es requerido.");
        if (string.IsNullOrWhiteSpace(request.Story)) throw new ArgumentException("Story es requerido.");
        ValidatePhotoFiles(request.Photos);

        var pet = new Pet(
            request.Name,
            request.Species,
            request.Breed,
            request.Gender,
            request.Temperament,
            request.Story,
            [],
            request.Available ?? false
        )
        {
            ShelterId = user.ShelterId.Value
        };

        _dbContext.Pets.Add(pet);
        await _dbContext.SaveChangesAsync();

        if (request.Photos != null && request.Photos.Count > 0)
        {
            var urls = new List<string>();
            for (int i = 0; i < request.Photos.Count; i++)
            {
                var url = await _storageService.UploadPetPhotoAsync(request.Photos[i], pet.Id, i + 1);
                urls.Add(url);
            }
            pet.Photos = urls;
            await _dbContext.SaveChangesAsync();
        }

        return ToResponse(pet);
    }

    public async Task<PetResponse?> GetPetByIdAsync(Guid petId, string? requesterUserId = null)
    {
        var pet = await _dbContext.Pets.FindAsync(petId);
        if (pet == null) return null;

        // Si no hay usuario (llamada interna legacy) retorna directo
        if (requesterUserId == null) return ToResponse(pet);

        var requester = await _userManager.FindByIdAsync(requesterUserId);
        if (requester == null) return ToResponse(pet);

        var isOwnerOrDev = await IsShelterOwnerOrDevAsync(requester);

        // User sin privilegios: oculta no disponibles
        if (!isOwnerOrDev && !pet.Available)
            return null;

        // ShelterOwner solo ve sus propias mascotas si no están disponibles? Permitir ver si es de su shelter, sino 404 para ocultar de otros shelters
        if (isOwnerOrDev && !pet.Available)
        {
            // Si es Dev puede ver todo
            var isDev = await _userManager.IsInRoleAsync(requester, "Dev");
            if (!isDev && requester.ShelterId != pet.ShelterId)
                return null;
        }

        return ToResponse(pet);
    }

    // Overload legacy sin userId para compatibilidad
    public Task<PetResponse?> GetPetByIdAsync(Guid petId) => GetPetByIdAsync(petId, null);

    public async Task<PagedResponse<PetResponse>> GetAllPetsAsync(int page, int pageSize, string? requesterUserId = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<Pet> query = _dbContext.Pets.AsNoTracking();

        if (requesterUserId != null)
        {
            var requester = await _userManager.FindByIdAsync(requesterUserId);
            if (requester != null)
            {
                var isOwnerOrDev = await IsShelterOwnerOrDevAsync(requester);
                if (!isOwnerOrDev)
                {
                    query = query.Where(p => p.Available);
                }
                else
                {
                    var isDev = await _userManager.IsInRoleAsync(requester, "Dev");
                    if (!isDev)
                    {
                        // ShelterOwner solo ve sus mascotas (incluye no disponibles)
                        if (requester.ShelterId != null)
                            query = query.Where(p => p.ShelterId == requester.ShelterId);
                        else
                            query = query.Where(p => p.Available);
                    }
                    // Dev ve todo (incluye no disponibles de todos los shelters)
                }
            }
            else
            {
                query = query.Where(p => p.Available);
            }
        }
        else
        {
            query = query.Where(p => p.Available);
        }

        query = query.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResponse<PetResponse>(
            items.Select(ToResponse),
            page,
            pageSize,
            totalCount,
            totalPages
        );
    }

    public async Task<PetResponse?> UpdatePetAsync(Guid petId, UpdatePetRequest request, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");
        if (user.ShelterId == null)
            throw new InvalidOperationException("No Shelter associated with your user");

        var pet = await _dbContext.Pets.FindAsync(petId);
        if (pet == null) return null;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        if (!isDev && pet.ShelterId != user.ShelterId)
            throw new UnauthorizedAccessException("No puedes modificar mascotas de otro refugio.");

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name no puede estar vacío.");
            if (request.Name.Length > 100) throw new ArgumentException("Name máximo 100 caracteres.");
            pet.Name = request.Name;
        }
        if (request.Species.HasValue) pet.Species = request.Species.Value;
        if (request.Breed != null)
        {
            if (string.IsNullOrWhiteSpace(request.Breed)) throw new ArgumentException("Breed no puede estar vacío.");
            pet.Breed = request.Breed;
        }
        if (request.Gender.HasValue) pet.Gender = request.Gender.Value;
        if (request.Temperament != null)
        {
            if (string.IsNullOrWhiteSpace(request.Temperament)) throw new ArgumentException("Temperament no puede estar vacío.");
            pet.Temperament = request.Temperament;
        }
        if (request.Story != null)
        {
            if (string.IsNullOrWhiteSpace(request.Story)) throw new ArgumentException("Story no puede estar vacío.");
            pet.Story = request.Story;
        }
        if (request.Available.HasValue) pet.Available = request.Available.Value;

        await _dbContext.SaveChangesAsync();
        return ToResponse(pet);
    }

    public async Task<PetResponse?> UpdatePetPhotoAsync(Guid petId, int photoIndex, IFormFile photo, string userId)
    {
        if (photoIndex < 1 || photoIndex > 3) throw new ArgumentException("PhotoIndex debe ser 1, 2 o 3.");
        ValidatePhotoFiles([photo]);

        var user = await GetUserOrThrowAsync(userId);
        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");
        if (user.ShelterId == null)
            throw new InvalidOperationException("No Shelter associated with your user");

        var pet = await _dbContext.Pets.FindAsync(petId);
        if (pet == null) return null;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        if (!isDev && pet.ShelterId != user.ShelterId)
            throw new UnauthorizedAccessException("No puedes modificar mascotas de otro refugio.");

        var url = await _storageService.UploadPetPhotoAsync(photo, pet.Id, photoIndex);

        var photos = pet.Photos.ToList();
        while (photos.Count < photoIndex) photos.Add("");
        photos[photoIndex - 1] = url;
        // Trim trailing empty
        photos = photos.Where((p, idx) => !string.IsNullOrEmpty(p) || idx < photos.FindLastIndex(x => !string.IsNullOrEmpty(x)) + 1).ToList();
        pet.Photos = photos.Where(p => !string.IsNullOrEmpty(p)).ToList();
        // Ensure max 3
        if (pet.Photos.Count > 3) pet.Photos = pet.Photos.Take(3).ToList();

        await _dbContext.SaveChangesAsync();
        return ToResponse(pet);
    }

    public async Task<bool> DeletePetAsync(Guid petId, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");
        if (user.ShelterId == null)
            throw new InvalidOperationException("No Shelter associated with your user");

        var pet = await _dbContext.Pets.FindAsync(petId);
        if (pet == null) return false;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        if (!isDev && pet.ShelterId != user.ShelterId)
            throw new UnauthorizedAccessException("No puedes borrar mascotas de otro refugio.");

        // Hard delete en cascada: Adoptions y Favorites se borran por Cascade configurado en PatitasDbContext
        _dbContext.Pets.Remove(pet);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
