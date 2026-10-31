namespace PatitasAPI.Core.Entities; 

public class Form
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string FileUrl { get; set; }

    // Foregin properties
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }

    // Navigation properties
    public ICollection<Pet> Pets { get; set; } = [];

    #pragma warning disable CS8618
    public Form() { }

    public Form(string name, string fileUrl, Guid shelterId)
    {
        Name = name;
        FileUrl = fileUrl;
        ShelterId = shelterId;
    }
}