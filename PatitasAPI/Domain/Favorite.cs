using System.Text.Json.Serialization;

namespace PatitasAPI.Domain;

public class Favorite
{
    [JsonPropertyName("idAdoptante")]
    public string AdopterId { get; set; }

    [JsonPropertyName("idMascota")]
    public string PetId { get; set; }

#pragma warning disable CS8618
    public Favorite() { }

    public Favorite(string adopterId, string petId)
    {
        AdopterId = adopterId;
        PetId = petId;
    }
}