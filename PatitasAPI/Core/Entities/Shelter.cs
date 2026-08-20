namespace PatitasAPI.Core.Entities; 

public class Shelter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public string Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? PhotoUrl { get; set; }
    public bool IsAvailable { get; set; } = false;
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
