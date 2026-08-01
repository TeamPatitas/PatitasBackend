namespace PatitasAPI.Core.Entities;

public enum Species
{
    OTHER,
    DOG,
    CAT,
}

public class Pet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public Species Species { get; set; }

    public string Breed { get; set; }

    public string Gender { get; set; }

    public string Temperament { get; set; }

    public string Story { get; set; }

    public List<string> Photos { get; set; } = [];

    public bool Aviable { get; set; } = false;

    // Foreign Variables
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }

    // Navigation Properties
    public ICollection<Adoption> Adoptions { get; set; } = new List<Adoption>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    #pragma warning disable CS8618
    public Pet() { }

    public Pet(string name, Species species, string breed, string gender, string temperament, string story, List<string> photos, bool aviable)
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
