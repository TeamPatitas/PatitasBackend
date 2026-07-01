using System.Text.Json.Serialization; 
namespace PatitasAPI.Domain; 

public class Adopter
{
    [JsonPropertyName("id_adoptante")]
    public string IdAdopter { get; set; }

    [JsonPropertyName("nombre")]
    public string Name { get; set; }

    [JsonPropertyName("correo")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("num_celular")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("edad")]
    public int Age { get; set; }

    [JsonPropertyName("sexo")]
    public string Gender { get; set; }

    #pragma warning disable CS8618
    // Esto es para evitar el warning de que las propiedades no se inicializan en el constructor xd.
    public Adopter() { }

    public Adopter(string idAdopter, string name, string email, string password,
                        string phoneNumber, int age, string gender)
    {
        IdAdopter = idAdopter;
        Name = name;
        Email = email;
        Password = password;
        PhoneNumber = phoneNumber;
        Age = age;
        Gender = gender;
    }
}
