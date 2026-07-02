using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities;

public class Pet
{
    [JsonPropertyName("id_mascota")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("especie")]
    public string Species { get; set; }

    [JsonPropertyName("raza")]
    public string Breed { get; set; }

    [JsonPropertyName("sexo")]
    public string Gender { get; set; }

    [JsonPropertyName("temperamento")]
    public string Temperament { get; set; }

    [JsonPropertyName("historia")]
    public string Story { get; set; }

    [JsonPropertyName("fotos")]
    public List<string> Photos { get; set; } = [];

    [JsonPropertyName("estado")]
    public bool Aviable { get; set; } = false;

    // Foreign Variables
    [JsonPropertyName("id_refugio")]
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }
    #pragma warning disable CS8618
    public Pet() { }

    public Pet(string name, string species, string breed, string gender, string temperament, string story, List<string> photos, bool aviable)
    {
        Name = name;
        Species = species;
        Breed = breed;
        Gender = gender;
        Temperament = temperament;
        Story = story;
        Photos = photos;
        Aviable = aviable;
    }
}