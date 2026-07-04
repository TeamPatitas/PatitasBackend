using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities; 

public class Favorite
{
    [JsonPropertyName("idFavorito")]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Variables
    [JsonPropertyName("idAdoptante")]
    public string AppUserId { get; set; }
    public AppUser AppUser { get; set; }

    [JsonPropertyName("idMascota")]
    public Guid PetId { get; set; }
    public Pet Pet { get; set; }

    #pragma warning disable CS8618
    public Favorite() { }

    public Favorite(string appUserId, Guid petId)
    {
        AppUserId = appUserId;
        PetId = petId;
    }
}