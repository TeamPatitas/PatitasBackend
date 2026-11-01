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

    private static ShelterResponse ToResponse(Shelter shelter, bool isOwner = false) => new ShelterResponse
    {
        Id = shelter.Id,
        Name = shelter.Name,
        PhoneNumber = shelter.PhoneNumber,
        IsAvailable = shelter.IsAvailable,
        Location = (shelter.ShowAddress || isOwner) && shelter.Latitude.HasValue && shelter.Longitude.HasValue
            ? new Location { Latitude = shelter.Latitude.Value, Longitude = shelter.Longitude.Value }
            : null,
        PhotoUrl = shelter.PhotoUrl,
        YapeQrCode = shelter.YapeQrCode,
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

        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name es requerido.");
        if (string.IsNullOrWhiteSpace(req.PhoneNumber)) throw new ArgumentException("PhoneNumber es requerido.");

        var shelter = new Shelter(
            req.Name.Trim(),
            req.PhoneNumber.Trim(),
            req.Latitude,
            req.Longitude,
            null
        )
        {
            ShowAddress = req.ShowAddress ?? true
        };

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

        if (req.YapeQrImage != null)
        {
            ValidatePhoto(req.YapeQrImage);
            shelter.YapeQrCode = await _storageService.UploadFileAsync(req.YapeQrImage, $"{shelter.Id}-yapeqrcode.webp");
            await _context.SaveChangesAsync();
        }

        user.ShelterId = shelter.Id;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException("Error al asignar refugio al usuario: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        await _context.Entry(shelter).Collection(s => s.Owners).LoadAsync();
        return ToResponse(shelter, isOwner: true);
    }

    private static void ValidatePhoto(IFormFile photo)
    {
        if (photo.Length == 0) throw new ArgumentException("Archivo vacío.");
        if (photo.Length > 10 * 1024 * 1024) throw new ArgumentException("Archivo excede 10MB.");
        var ct = photo.ContentType.ToLowerInvariant();
        if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
            throw new ArgumentException($"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp.");
    }

    private static void ValidatePdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("El archivo PDF es requerido y no puede estar vacío.");
        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("El archivo PDF excede el límite máximo de 5MB.");

        var ct = file.ContentType.ToLowerInvariant();
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ct != "application/pdf" || ext != ".pdf")
            throw new ArgumentException("Solo se permiten archivos en formato .pdf.");
    }

    public async Task<FormResponse> UploadFormAsync(UploadFormRequest request, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        var isOwnerOrDev = await _userManager.IsInRoleAsync(user, "ShelterOwner") || await _userManager.IsInRoleAsync(user, "Dev");
        if (!isOwnerOrDev)
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null)
            throw new InvalidOperationException("No existe un refugio asociado a tu usuario.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name es requerido.");
        if (request.Name.Trim().Length > 100)
            throw new ArgumentException("Name máximo 100 caracteres.");

        ValidatePdf(request.File);

        var shelterId = user.ShelterId.Value;
        var currentFormsCount = await _context.Forms.CountAsync(f => f.ShelterId == shelterId);
        if (currentFormsCount >= 5)
            throw new InvalidOperationException("Se ha alcanzado el límite máximo de 5 formularios por refugio.");

        var form = new Form(request.Name.Trim(), string.Empty, shelterId);
        var key = $"shelters/{shelterId}/forms/{form.Id}.pdf";
        form.FileUrl = await _storageService.UploadPdfAsync(request.File, key);

        _context.Forms.Add(form);
        await _context.SaveChangesAsync();

        return new FormResponse
        {
            Id = form.Id,
            Name = form.Name,
            FileUrl = form.FileUrl,
            CreatedAt = form.CreatedAt,
            ShelterId = form.ShelterId
        };
    }

    public async Task<PagedResponse<FormResponse>> GetFormsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        var isOwnerOrDev = await _userManager.IsInRoleAsync(user, "ShelterOwner") || await _userManager.IsInRoleAsync(user, "Dev");
        if (!isOwnerOrDev)
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null)
            throw new InvalidOperationException("No existe un refugio asociado a tu usuario.");

        var shelterId = user.ShelterId.Value;
        var forms = await _context.Forms
            .AsNoTracking()
            .Where(f => f.ShelterId == shelterId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FormResponse
            {
                Id = f.Id,
                Name = f.Name,
                FileUrl = f.FileUrl,
                CreatedAt = f.CreatedAt,
                ShelterId = f.ShelterId
            })
            .ToListAsync();

        return new PagedResponse<FormResponse>
        {
            Items = forms,
            Page = 1,
            PageSize = 5,
            TotalCount = forms.Count,
            TotalPages = forms.Count > 0 ? 1 : 0
        };
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

        var isOwner = requesterUserId != null && shelter.Owners.Any(o => o.Id == requesterUserId);

        if (!shelter.IsAvailable)
        {
            if (requesterUserId == null) return null;
            var user = await _userManager.FindByIdAsync(requesterUserId);
            if (user == null) return null;
            var isDev = await _userManager.IsInRoleAsync(user, "Dev");
            if (!isDev && !isOwner) return null;
        }

        return ToResponse(shelter, isOwner);
    }

    public async Task<ShelterResponse?> UpdateAsync(Guid shelterId, UpdateShelterRequest request, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Usuario no encontrado");
        var shelter = await _context.Shelters.Include(s => s.Owners).FirstOrDefaultAsync(s => s.Id == shelterId);
        if (shelter == null) return null;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        var isOwner = user.ShelterId == shelterId || shelter.Owners.Any(o => o.Id == userId);
        if (!isDev && !isOwner) throw new UnauthorizedAccessException("No puedes modificar este refugio");

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name no puede estar vacío");
            shelter.Name = request.Name.Trim();
        }
        if (request.PhoneNumber != null)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber)) throw new ArgumentException("PhoneNumber no puede estar vacío");
            shelter.PhoneNumber = request.PhoneNumber.Trim();
        }
        if (request.Latitude.HasValue) shelter.Latitude = request.Latitude;
        if (request.Longitude.HasValue) shelter.Longitude = request.Longitude;
        if (request.ShowAddress.HasValue) shelter.ShowAddress = request.ShowAddress.Value;

        var photo = request.Photo;
        if (photo != null)
        {
            ValidatePhoto(photo);
            var key = $"shelters/{shelter.Id}.webp";
            shelter.PhotoUrl = await _storageService.UploadFileAsync(photo, key);
        }

        if (request.YapeQrImage != null)
        {
            ValidatePhoto(request.YapeQrImage);
            shelter.YapeQrCode = await _storageService.UploadFileAsync(request.YapeQrImage, $"{shelter.Id}-yapeqrcode.webp");
        }

        await _context.SaveChangesAsync();
        return ToResponse(shelter, isOwner);
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

    public async Task<FormResponse?> GetFormByIdAsync(Guid formId)
    {
        var form = await _context.Forms
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == formId);
        if (form == null) return null;

        return new FormResponse
        {
            Id = form.Id,
            Name = form.Name,
            FileUrl = form.FileUrl,
            CreatedAt = form.CreatedAt,
            ShelterId = form.ShelterId
        };
    }
}
