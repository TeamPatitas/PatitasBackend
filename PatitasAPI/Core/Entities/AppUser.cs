using Microsoft.AspNetCore.Identity;
using PatitasAPI.Core.Utils;
namespace PatitasAPI.Core.Entities; 

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? PhotoUrl { get; set; }

    // Foreign Variables
    public Guid? ShelterId { get; set; }
    public Shelter? Shelter { get; set; }

    // Navigation Properties
    public ICollection<Adoption> Adoptions { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}