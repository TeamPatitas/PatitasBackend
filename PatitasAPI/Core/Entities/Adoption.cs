using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities; 

public enum AdoptionStatus
{
    Requested,
    Approved,
    Rejected
}

public class Adoption
{
    [JsonPropertyName("id_adopcion")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonPropertyName("estado")]
    public AdoptionStatus Status { get; set; } 
    [JsonPropertyName("notas")]
    public string Notes { get; set; }

    // Foreign Variables
    [JsonPropertyName("id_mascota")]
    public Guid PetId { get; set; }
    public Pet Pet { get; set; }

    [JsonPropertyName("id_adoptante")]
    public Guid AdopterId { get; set; }
    public AppUser Adopter { get; set; }

    #pragma warning disable CS8618
    public Adoption() { }

    public Adoption(Guid adopterId, Guid petId, AdoptionStatus status, string notes)
    {
        AdopterId = adopterId;
        PetId = petId;
        Status = status;
        Notes = notes;
    }
}