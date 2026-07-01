using System.Text.Json.Serialization;

namespace PatitasAPI.Domain;

public class Shelter
{
    [JsonPropertyName("id_refugio")]
    public string ShelterId { get; set; }

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("direccion")]
    public string Address { get; set; }

    [JsonPropertyName("latitud")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitud")]
    public double Longitude { get; set; }

    [JsonPropertyName("correo")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("num_celular")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("foto")]
    public string PhotoUrl { get; set; }

#pragma warning disable CS8618
    public Shelter() { }

    public Shelter(string shelterId, string name, string address, double latitude, double longitude,
                   string email, string password, string phoneNumber, string photoUrl)
    {
        ShelterId = shelterId;
        Name = name;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        Email = email;
        Password = password;
        PhoneNumber = phoneNumber;
        PhotoUrl = photoUrl;
    }
}