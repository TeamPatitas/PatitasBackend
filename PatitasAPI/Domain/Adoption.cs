using System.Text.Json.Serialization;

namespace PatitasAPI.Domain;

public enum AdoptionStatus
{
    Requested,
    Approved,
    Rejected
}

public class Adoption
{
    [JsonPropertyName("id_adopcion")]
    public string AdoptionId { get; set; }

    [JsonPropertyName("id_adoptante")]
    public string AdopterId { get; set; }

    [JsonPropertyName("id_mascota")]
    public string PetId { get; set; }

    [JsonPropertyName("id_refugio")]
    public string ShelterId { get; set; }

    [JsonPropertyName("estado")]
    public AdoptionStatus Status { get; set; } 

    [JsonPropertyName("notas")]
    public string Notes { get; set; }

    #pragma warning disable CS8618
    public Adoption() { }

    public Adoption(string adoptionId, string adopterId, string petId, string shelterId, AdoptionStatus status, string notes)
    {
        AdoptionId = adoptionId;
        AdopterId = adopterId;
        PetId = petId;
        ShelterId = shelterId;
        Status = status;
        Notes = notes;
    }
}