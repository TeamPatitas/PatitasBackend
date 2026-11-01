namespace PatitasAPI.Core.Entities; 

public class Shelter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? PhotoUrl { get; set; }
    public string? YapeQrCode { get; set; }
    public bool ShowAddress { get; set; } = true;
    public bool IsAvailable { get; set; } = false;
    // Navigation Variables
    public ICollection<Pet> Pets { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<AppUser> Owners { get; set; } = [];
    public ICollection<Form> Forms { get; set; } = [];

    #pragma warning disable CS8618
    public Shelter() { }

    public Shelter(string name, string phoneNumber, double? latitude = null, double? longitude = null, string? photoUrl = null)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Latitude = latitude;
        Longitude = longitude;
        PhotoUrl = photoUrl;
    }
}
