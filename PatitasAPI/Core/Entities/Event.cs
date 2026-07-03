using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities; 

public class Event
{
    [JsonPropertyName("idEvento")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("fecha")]
    public DateTime Date { get; set; }

    [JsonPropertyName("descripcion")]
    public string Description { get; set; }

    [JsonPropertyName("fotoUrl")]
    public string PhotoUrl { get; set; }

    // Foreign Variables
    [JsonPropertyName("id_refugio")]
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }

    #pragma warning disable CS8618
    public Event() { }

    public Event(string name, DateTime date, string description, string photoUrl)
    {
        Name = name;
        Date = date;
        Description = description;
        PhotoUrl = photoUrl;
    }
}