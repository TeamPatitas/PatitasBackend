using Microsoft.AspNetCore.Identity;
namespace PatitasAPI.Core.Entities; 

public enum Gender
{
    MALE,
    FEMALE
}

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public string? PhotoUrl { get; set; }

    // Foreign Variables
    public Guid? ShelterId { get; set; }
    public Shelter? Shelter { get; set; }

    // Navigation Properties
    public ICollection<Adoption> Adoptions { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}