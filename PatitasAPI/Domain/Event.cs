using System.Text.Json.Serialization;

namespace PatitasAPI.Domain;

public class Event
{
    [JsonPropertyName("idEvento")]
    public string EventId { get; set; }

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("fecha")]
    public string Date { get; set; }

    [JsonPropertyName("descripcion")]
    public string Description { get; set; }

    [JsonPropertyName("fotoUrl")]
    public string PhotoUrl { get; set; }

#pragma warning disable CS8618
    public Event() { }

    public Event(string eventId, string name, string date, string description, string photoUrl)
    {
        EventId = eventId;
        Name = name;
        Date = date;
        Description = description;
        PhotoUrl = photoUrl;
    }
}