namespace PatitasAPI.Core.Entities;
using PatitasAPI.Core.Utils;

public class Pet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public Species Species { get; set; }

    public string Breed { get; set; }

    public Gender Gender { get; set; }

    public string Temperament { get; set; }

    public string Story { get; set; }

    public List<string> Photos { get; set; } = [];

    public bool Available { get; set; } = false;

    // Foreign Variables
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }

    // Navigation Properties
    public ICollection<Adoption> Adoptions { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];

    #pragma warning disable CS8618
    public Pet() { }

    public Pet(string name, Species species, string breed, Gender gender, string temperament, string story, List<string> photos, bool available)
    {
        Name = name;
        Species = species;
        Breed = breed;
        Gender = gender;
        Temperament = temperament;
        Story = story;
        Photos = photos;
        Available = available;
    }
}
