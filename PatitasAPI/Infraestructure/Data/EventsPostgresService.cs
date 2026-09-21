using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Data;

public class EventsPostgresService(PatitasDbContext dbContext, UserManager<AppUser> userManager, IStorageService storageService) : IEventService
{
    private readonly PatitasDbContext _dbContext = dbContext;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IStorageService _storageService = storageService;

    private static EventResponse ToResponse(Event e, bool isYours) => new()
    {
        Id = e.Id,
        Name = e.Name,
        EventDate = e.EventDate,
        CreatedAt = e.CreatedAt,
        Description = e.Description,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        PhotoUrl = e.PhotoUrl,
        IsActive = e.IsActive,
        ShelterId = e.ShelterId,
        IsYours = isYours
    };

    private static EventSummaryResponse ToSummaryResponse(Event e, bool isYours) => new()
    {
        Id = e.Id,
        Name = e.Name,
        EventDate = e.EventDate,
        PhotoUrl = e.PhotoUrl,
        IsActive = e.IsActive,
        ShelterId = e.ShelterId,
        IsYours = isYours
    };

    private static bool ComputeIsYours(AppUser? requester, Event e)
    {
        if (requester?.ShelterId == null) return false;
        return requester.ShelterId == e.ShelterId;
    }

    private static void ValidatePhoto(IFormFile photo)
    {
        if (photo.Length == 0) throw new ArgumentException("Archivo vacío.");
        if (photo.Length > 10 * 1024 * 1024) throw new ArgumentException("Archivo excede 10MB.");
        var ct = photo.ContentType.ToLowerInvariant();
        if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp" && ct != "image/jpg")
            throw new ArgumentException($"Tipo de imagen no permitido: {ct}. Use jpeg/png/webp.");
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

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null)
            throw new InvalidOperationException("No eres dueño de ningún refugio");

        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name es requerido.");
        if (request.EventDate == default) throw new ArgumentException("EventDate es requerido.");

        var e = new Event
        {
            Name = request.Name.Trim(),
            EventDate = request.EventDate.ToUniversalTime(),
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = request.IsActive ?? false,
            ShelterId = user.ShelterId.Value
        };

        _dbContext.Events.Add(e);
        await _dbContext.SaveChangesAsync();

        if (request.Image != null)
        {
            ValidatePhoto(request.Image);
            e.PhotoUrl = await _storageService.UploadFileAsync(request.Image, $"events/{e.Id}.webp");
            await _dbContext.SaveChangesAsync();
        }

        return ToResponse(e, true);
    }

    public async Task<EventResponse?> GetEventByIdAsync(Guid eventId, string? requesterUserId = null)
    {
        var e = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        if (e == null) return null;

        if (requesterUserId == null)
            return e.IsActive ? ToResponse(e, false) : null;

        var requester = await _userManager.FindByIdAsync(requesterUserId);
        if (requester == null)
            return e.IsActive ? ToResponse(e, false) : null;

        var isDev = await _userManager.IsInRoleAsync(requester, "Dev");
        if (isDev) return ToResponse(e, ComputeIsYours(requester, e));

        var isYours = ComputeIsYours(requester, e);
        if (!e.IsActive && !isYours) return null;

        return ToResponse(e, isYours);
    }

    public async Task<PagedResponse<EventSummaryResponse>> GetAllEventsAsync(int page, int pageSize, string? requesterUserId = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<Event> query = _dbContext.Events.AsNoTracking();
        Guid? requesterShelterId = null;

        if (requesterUserId != null)
        {
            var requester = await _userManager.FindByIdAsync(requesterUserId);
            if (requester != null)
            {
                var isOwnerOrDev = await IsShelterOwnerOrDevAsync(requester);
                if (!isOwnerOrDev)
                {
                    query = query.Where(e => e.IsActive);
                }
                else
                {
                    var isDev = await _userManager.IsInRoleAsync(requester, "Dev");
                    if (!isDev)
                    {
                        requesterShelterId = requester.ShelterId;
                        if (requesterShelterId != null)
                            query = query.Where(e => e.IsActive || e.ShelterId == requesterShelterId);
                        else
                            query = query.Where(e => e.IsActive);
                    }
                    else
                    {
                        requesterShelterId = requester.ShelterId;
                    }
                }
            }
            else
            {
                query = query.Where(e => e.IsActive);
            }
        }
        else
        {
            query = query.Where(e => e.IsActive);
        }

        query = query.OrderBy(e => e.EventDate);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResponse<EventSummaryResponse>
        {
            Items = items.Select(e => ToSummaryResponse(e, requesterShelterId != null && requesterShelterId == e.ShelterId)),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<EventResponse?> UpdateEventAsync(Guid eventId, UpdateEventRequest request, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null)
            throw new InvalidOperationException("No eres dueño de ningún refugio");

        var e = await _dbContext.Events.FindAsync(eventId);
        if (e == null) return null;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        if (!isDev && e.ShelterId != user.ShelterId)
            throw new UnauthorizedAccessException("No puedes modificar eventos de otro refugio.");

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name no puede estar vacío.");
            e.Name = request.Name.Trim();
        }
        if (request.EventDate.HasValue) e.EventDate = request.EventDate.Value.ToUniversalTime();
        if (request.Description != null) e.Description = request.Description;
        if (request.Latitude != null) e.Latitude = request.Latitude;
        if (request.Longitude != null) e.Longitude = request.Longitude;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;

        if (request.Image != null)
        {
            ValidatePhoto(request.Image);
            e.PhotoUrl = await _storageService.UploadFileAsync(request.Image, $"events/{e.Id}.webp");
        }

        await _dbContext.SaveChangesAsync();
        return ToResponse(e, ComputeIsYours(user, e));
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!await IsShelterOwnerOrDevAsync(user))
            throw new UnauthorizedAccessException("Se requiere rol ShelterOwner.");

        if (user.ShelterId == null)
            throw new InvalidOperationException("No eres dueño de ningún refugio");

        var e = await _dbContext.Events.FindAsync(eventId);
        if (e == null) return false;

        var isDev = await _userManager.IsInRoleAsync(user, "Dev");
        if (!isDev && e.ShelterId != user.ShelterId)
            throw new UnauthorizedAccessException("No puedes borrar eventos de otro refugio.");

        _dbContext.Events.Remove(e);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
