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
}