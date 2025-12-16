using System.ComponentModel.DataAnnotations;

namespace EventRsvp.Application.DTOs;

public class CreateRsvpRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    public bool BringingDish { get; set; }

    public List<string> Dishes { get; set; } = new();

    public bool WhiteElephant { get; set; }
}

