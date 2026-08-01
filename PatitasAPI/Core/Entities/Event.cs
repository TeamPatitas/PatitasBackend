namespace PatitasAPI.Core.Entities; 

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public DateTime Date { get; set; }

    public string Description { get; set; }

    public string PhotoUrl { get; set; }

    // Foreign Variables
    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; }

    #pragma warning disable CS8618
    public Event() { }

    public Event(string name, DateTime date, string description, string photoUrl)
    {
        Name = name;
        Date = date;
        Description = description;
        PhotoUrl = photoUrl;
    }
}
