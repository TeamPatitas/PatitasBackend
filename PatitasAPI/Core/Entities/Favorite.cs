using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities; 

public class Favorite
{
    [JsonPropertyName("idFavorito")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Variables
    [JsonPropertyName("idAdoptante")]
    public Guid AdopterId { get; set; }
    public AppUser Adopter { get; set; }

    [JsonPropertyName("idMascota")]
    public Guid PetId { get; set; }
    public Pet Pet { get; set; }

    #pragma warning disable CS8618
    public Favorite() { }

    public Favorite(Guid adopterId, Guid petId)
    {
        AdopterId = adopterId;
        PetId = petId;
    }
}