using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Entities;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Data;

public class ShelterPostgresService(PatitasDbContext context, UserManager<AppUser> userManager) : IShelterService
{
    private readonly PatitasDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;

    private static ShelterResponse ToResponse(Shelter shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Address,
        shelter.IsAvailable,
        shelter.Latitude,
        shelter.Longitude,
        shelter.PhotoUrl
    );

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
            req.PhotoUrl
        );

        _context.Shelters.Add(shelter);
        await _context.SaveChangesAsync();

        user.ShelterId = shelter.Id;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException("Error al asignar refugio al usuario: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        return ToResponse(shelter);
    }

    public async Task<ShelterResponse?> EnableAsync(Guid shelterId)
    {
        var shelter = await _context.Shelters.FindAsync(shelterId);
        if (shelter == null) return null;
        if (shelter.IsAvailable)
            throw new InvalidOperationException("El refugio ya está habilitado");
        shelter.IsAvailable = true;
        await _context.SaveChangesAsync();
        return ToResponse(shelter);
    }

    public async Task<ShelterResponse?> DisableAsync(Guid shelterId)
    {
        var shelter = await _context.Shelters.FindAsync(shelterId);
        if (shelter == null) return null;
        if (!shelter.IsAvailable)
            throw new InvalidOperationException("El refugio ya está deshabilitado");
        shelter.IsAvailable = false;
        await _context.SaveChangesAsync();
        return ToResponse(shelter);
    }

    public async Task<PagedResponse<ShelterResponse>> GetAllAsync(int page, int pageSize, string? requesterUserId = null)
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

        return new PagedResponse<ShelterResponse>(
            items.Select(ToResponse),
            page,
            pageSize,
            totalCount,
            totalPages
        );
    }

    public async Task<ShelterResponse?> GetByIdAsync(Guid shelterId, string? requesterUserId = null)
    {
        var shelter = await _context.Shelters.FindAsync(shelterId);
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
}
