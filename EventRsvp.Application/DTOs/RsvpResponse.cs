namespace EventRsvp.Application.DTOs;

public class RsvpResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool BringingDish { get; set; }
    public List<string> Dishes { get; set; } = new();
    public bool WhiteElephant { get; set; }
    public DateTime CreatedAt { get; set; }
}

