using System.Text.Json.Serialization;

namespace PatitasAPI.Domain;

public enum PetStatus
{
    Available,
    InProcess,
    Adopted
}

public class Pet
{
    [JsonPropertyName("id_mascota")]
    public string PetId { get; set; }

    [JsonPropertyName("id_refugio")]
    public string ShelterId { get; set; }

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("especie")]
    public string Species { get; set; }

    [JsonPropertyName("raza")]
    public string Breed { get; set; }

    [JsonPropertyName("sexo")]
    public string Sex { get; set; }

    [JsonPropertyName("edad")]
    public int Age { get; set; }

    [JsonPropertyName("temperamento")]
    public string Temperament { get; set; }

    [JsonPropertyName("historia")]
    public string Story { get; set; }

    [JsonPropertyName("fotos")]
    public List<string> Photos { get; set; } = [];

    [JsonPropertyName("estado")]
    public PetStatus Status { get; set; }

    #pragma warning disable CS8618
    public Pet() { }

    public Pet(string petId, string shelterId, string name, string species, string breed, string sex,
               int age, string temperament, string story, List<string>? photos, PetStatus status)
    {
        PetId = petId;
        ShelterId = shelterId;
        Name = name;
        Species = species;
        Breed = breed;
        Sex = sex;
        Age = age;
        Temperament = temperament;
        Story = story;
        
        Photos = photos ?? []; 
        Status = status;
    }
}