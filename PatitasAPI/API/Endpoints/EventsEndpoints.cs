using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.API.Endpoints;

public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/event").WithTags("Eventos");

        group.MapPost("/", async ([FromForm] CreateEventRequest request, ClaimsPrincipal user, IEventService eventService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var created = await eventService.CreateEventAsync(request, userId);
                return Results.Created($"/event/{created.Id}", created);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("CreateEvent")
        .WithDescription("Crea un nuevo evento asociado al refugio del usuario.")
        .Produces<EventResponse>(StatusCodes.Status201Created)
        .RequireAuthorization("ShelterOwner")
        .DisableAntiforgery();

        group.MapGet("/", async (int? page, int? pageSize, ClaimsPrincipal user, IEventService eventService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await eventService.GetAllEventsAsync(page ?? 1, pageSize ?? 20, userId);
            return Results.Ok(result);
        }).WithName("GetAllEvents")
        .WithDescription("Obtiene todos los eventos. Los usuarios ven solo los eventos activos ($code{IsActive == true}), dueños ven activos globales más inactivos propios, devs ven todo.")
        .Produces<PagedResponse<EventSummaryResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapGet("/{eventId:guid}", async (Guid eventId, ClaimsPrincipal user, IEventService eventService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await eventService.GetEventByIdAsync(eventId, userId);
            if (result == null) return Results.NotFound();
            return Results.Ok(result);
        }).WithName("GetEventById")
        .WithDescription("Obtiene los detalles de un evento por su ID, los Devs pueden obtener cualquier evento, los dueños de refugio pueden obtener cualquier evento activo o inactivo de su refugio, y los usuarios normales solo pueden obtener eventos activos.")
        .Produces<EventResponse>(StatusCodes.Status200OK)
        .RequireAuthorization("User");

        group.MapPatch("/{eventId:guid}", async (Guid eventId, [FromForm] UpdateEventRequest request, ClaimsPrincipal user, IEventService eventService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var updated = await eventService.UpdateEventAsync(eventId, request, userId);
                if (updated == null) return Results.NotFound();
                return Results.Ok(updated);
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("UpdateEvent")
        .WithDescription("Actualiza los datos de un evento del refugio del usuario.")
        .Produces<EventResponse>(StatusCodes.Status200OK)
        .RequireAuthorization("ShelterOwner")
        .DisableAntiforgery();

        group.MapDelete("/{eventId:guid}", async (Guid eventId, ClaimsPrincipal user, IEventService eventService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Results.Unauthorized();
            try
            {
                var deleted = await eventService.DeleteEventAsync(eventId, userId);
                if (!deleted) return Results.NotFound(new { message = "Event not found" });
                return Results.Ok("Evento Borrado");
            }
            catch (UnauthorizedAccessException ex) { return Results.Json(new { message = ex.Message }, statusCode: 403); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("DeleteEvent")
        .WithDescription("Elimina un evento. Si eres dev puedes eliminar cualquiera, si no solo los de tu refugio.")
        .Produces<string>(StatusCodes.Status200OK)
        .RequireAuthorization("ShelterOwner");
    }
}
