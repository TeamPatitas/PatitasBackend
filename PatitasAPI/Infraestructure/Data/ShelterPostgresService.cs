using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Data;

public class ShelterPostgresService(PatitasDbContext context, UserManager<AppUser> userManager, IStorageService storageService) : IShelterService
{
    private readonly PatitasDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IStorageService _storageService = storageService;

    private static ShelterResponse ToResponse(Shelter shelter) => new ShelterResponse
    {
        Id = shelter.Id,
        Name = shelter.Name,
        Address = shelter.Address,
        IsAvailable = shelter.IsAvailable,
        Latitude = shelter.Latitude,
        Longitude = shelter.Longitude,
        PhotoUrl = shelter.PhotoUrl,
        Owners = shelter.Owners.Select(o => Guid.Parse(o.Id)).ToList()
    };

    private static ShelterSummaryResponse ToSummaryResponse(Shelter shelter) => new ShelterSummaryResponse
    {
        Id = shelter.Id,
        Name = shelter.Name,
        PhotoUrl = shelter.PhotoUrl,
        IsAvailable = shelter.IsAvailable
    };

    public async Task<ShelterResponse> CreateShelterAsync(CreateShelterRequest req, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado");
        if (user.ShelterId != null)
            throw new InvalidOperationException("Ya tienes un refugio registrado. Solo se permite uno por usuario.");

        var shelter = new Shelter(
            req.Name,
            req.Address,
            req.Latitude,
            req.Longitude,
            null
        );

        _context.Shelters.Add(shelter);
        await _context.SaveChangesAsync();

        var photo = req.Photo;
        if (photo != null)
        {
            ValidatePhoto(photo);
            var key = $"shelters/{shelter.Id}.webp";
            shelter.PhotoUrl = await _storageService.UploadFileAsync(photo, key);
            await _context.SaveChangesAsync();
        }

        user.ShelterId = shelter.Id;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException("Error al asignar refugio al usuario: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        await _context.Entry(shelter).Collection(s => s.Owners).LoadAsync();
        return ToResponse(shelter);
    }

    private static void ValidatePhoto(IFormFile photo)
    {
        if (photo.Length == 0) throw new ArgumentException("Archivo vacío.");
        if (photo.Length > 10 * 1024 * 1024) throw new ArgumentException("Archivo excede 10MB.");
        var ct = photo.ContentType.ToLowerInvariant();
        if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
            throw new ArgumentException($"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp.");
    }

    public async Task<ShelterResponse?> EnableAsync(Guid shelterId)
    {
        var shelter = await _context.Shelters.Include(s => s.Owners).FirstOrDefaultAsync(s => s.Id == shelterId);
        if (shelter == null) return null;
        if (shelter.IsAvailable)
            throw new InvalidOperationException("El refugio ya está habilitado");
        shelter.IsAvailable = true;
        await _context.SaveChangesAsync();
        foreach (var owner in shelter.Owners)
        {
            if (!await _userManager.IsInRoleAsync(owner, "ShelterOwner"))
                await _userManager.AddToRoleAsync(owner, "ShelterOwner");
        }
        return ToResponse(shelter);
    }

    public async Task<ShelterResponse?> DisableAsync(Guid shelterId)
    {
        var shelter = await _context.Shelters.Include(s => s.Owners).FirstOrDefaultAsync(s => s.Id == shelterId);
        if (shelter == null) return null;
        if (!shelter.IsAvailable)
            throw new InvalidOperationException("El refugio ya está deshabilitado");
        shelter.IsAvailable = false;
        await _context.SaveChangesAsync();
        foreach (var owner in shelter.Owners)
        {
            if (await _userManager.IsInRoleAsync(owner, "ShelterOwner"))
                await _userManager.RemoveFromRoleAsync(owner, "ShelterOwner");
        }
        return ToResponse(shelter);
    }

    public async Task<PagedResponse<ShelterSummaryResponse>> GetAllAsync(int page, int pageSize, string? requesterUserId = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<Shelter> query = _context.Shelters.AsNoTracking().OrderBy(s => s.Name);

        if (requesterUserId != null)
        {
            var user = await _userManager.FindByIdAsync(requesterUserId);
            var isDev = user != null && await _userManager.IsInRoleAsync(user, "Dev");
            if (!isDev)
            {
                query = query.Where(s => s.IsAvailable);
            }
        }
        else
        {
            query = query.Where(s => s.IsAvailable);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResponse<ShelterSummaryResponse>
        {
            Items = items.Select(ToSummaryResponse),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<ShelterResponse?> GetByIdAsync(Guid shelterId, string? requesterUserId = null)
    {
        var shelter = await _context.Shelters.Include(s => s.Owners).FirstOrDefaultAsync(s => s.Id == shelterId);
        if (shelter == null) return null;

        if (!shelter.IsAvailable)
        {
            if (requesterUserId == null) return null;
            var user = await _userManager.FindByIdAsync(requesterUserId);
            if (user == null) return null;
            var isDev = await _userManager.IsInRoleAsync(user, "Dev");
            var isOwner = user.ShelterId == shelter.Id;
            if (!isDev && !isOwner) return null;
        }

        return ToResponse(shelter);
    }

    public async Task<ShelterResponse?> UpdateAsync(Guid shelterId, UpdateShelterRequest request, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado");
        var shelter = await _context.Shelters.Include(s => s.Owners).FirstOrDefaultAsync(s => s.Id == shelterId);
        if (shelter == null) return null;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        var isOwner = user.ShelterId == shelterId;
        if (!isDev && !isOwner) throw new UnauthorizedAccessException("No puedes modificar este refugio");

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name no puede estar vacío");
            shelter.Name = request.Name;
        }
        if (request.Address != null)
        {
            if (string.IsNullOrWhiteSpace(request.Address)) throw new ArgumentException("Address no puede estar vacío");
            shelter.Address = request.Address;
        }
        if (request.Latitude.HasValue) shelter.Latitude = request.Latitude;
        if (request.Longitude.HasValue) shelter.Longitude = request.Longitude;

        var photo = request.Photo;
        if (photo != null)
        {
            ValidatePhoto(photo);
            var key = $"shelters/{shelter.Id}.webp";
            shelter.PhotoUrl = await _storageService.UploadFileAsync(photo, key);
        }

        await _context.SaveChangesAsync();
        return ToResponse(shelter);
    }

    public async Task<bool> DeleteAsync(Guid shelterId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado");
        var shelter = await _context.Shelters.FindAsync(shelterId);
        if (shelter == null) return false;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        var isOwner = user.ShelterId == shelterId;
        if (!isDev && !isOwner) throw new UnauthorizedAccessException("No puedes borrar este refugio");

        foreach (var owner in shelter.Owners)
        {
            if (await _userManager.IsInRoleAsync(owner, "ShelterOwner"))
                await _userManager.RemoveFromRoleAsync(owner, "ShelterOwner");
        }

        _context.Shelters.Remove(shelter);
        await _context.SaveChangesAsync();
        return true;
    }
}
