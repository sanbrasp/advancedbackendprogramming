using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.DTOs;

public class CreateTicketRequestDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    
    [Required]
    public string User { get; set; } = string.Empty;
    
    public DateTime DateOfIssue { get; set; } = DateTime.UtcNow;
    
    public DateTime DateOfDeadline { get; set; } = DateTime.UtcNow.AddDays(14);
    
    [Required, StringLength(500)]
    public string Description { get; set; } =  string.Empty;
}