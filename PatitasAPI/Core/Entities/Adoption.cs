using PatitasAPI.Core.Utils;
namespace PatitasAPI.Core.Entities; 

public class Adoption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AdoptionStatus Status { get; set; } 
    public string Notes { get; set; }
    
    // Foreign Variables
    public string AppUserId { get; set; }
    public AppUser AppUser { get; set; }

    public Guid PetId { get; set; }
    public Pet Pet { get; set; }
    
    #pragma warning disable CS8618
    public Adoption() {}
    public Adoption(string appUserId, Guid petId, AdoptionStatus status, string notes)
    {
        AppUserId = appUserId;
        PetId = petId;
        Status = status;
        Notes = notes;
    }
}
