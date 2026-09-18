namespace PatitasAPI.Core.Entities; 

public class Favorite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Variables
    public string AppUserId { get; set; }
    public AppUser AppUser { get; set; }

    public Guid PetId { get; set; }
    public Pet Pet { get; set; }

    #pragma warning disable CS8618
    public Favorite() { }

    public Favorite(string appUserId, Guid petId)
    {
        AppUserId = appUserId;
        PetId = petId;
    }
}
