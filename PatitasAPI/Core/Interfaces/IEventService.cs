using PatitasAPI.Core.DTOs;

namespace PatitasAPI.Core.Interfaces;
public interface IEventService {
    Task<EventResponse> CreateEventAsync(CreateEventRequest request, string userId);
    Task<EventResponse?> GetEventByIdAsync(Guid eventId, string? requesterUserId = null);
    Task<PagedResponse<EventSummaryResponse>> GetAllEventsAsync(int page, int pageSize, string? requesterUserId = null);
    Task<EventResponse?> UpdateEventAsync(Guid eventId, UpdateEventRequest request, string userId);
    Task<bool> DeleteEventAsync(Guid eventId, string userId);
}