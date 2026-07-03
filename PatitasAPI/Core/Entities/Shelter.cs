using System.Text.Json.Serialization;

namespace PatitasAPI.Core.Entities; 

public class Shelter
{
    [JsonPropertyName("id_refugio")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("direccion")]
    public string Address { get; set; }

    [JsonPropertyName("latitud")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitud")]
    public double? Longitude { get; set; }

    [JsonPropertyName("foto")]
    public string PhotoUrl { get; set; }

    // Navigation Variables
    public ICollection<Pet> Pets { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<AppUser> Owners { get; set; } = [];

    #pragma warning disable CS8618
    public Shelter() { }

    public Shelter(string name, string address, double? latitude, double? longitude, string photoUrl)
    {
        Name = name;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        PhotoUrl = photoUrl;
    }
}