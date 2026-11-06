using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Settings;

public class TicketSettings
{
    public const string SectionName = "Tickets";
    
    [Range(1, int.MaxValue)]
    public int MaxTicketsReturned { get; set; }
    [Range(2, 500)]
    public int MaxDescriptionLength { get; set; }
}