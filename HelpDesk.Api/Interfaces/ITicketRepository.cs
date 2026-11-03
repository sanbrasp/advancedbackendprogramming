using HelpDesk.Api.Records;

namespace HelpDesk.Api.Interfaces;

public interface ITicketRepository
{
    IEnumerable<TicketRecord> GetAll();
    
    bool TryGet(int id, out TicketRecord? ticket);
    
    bool TryAdd(TicketRecord addedTicket);
    
    bool TryRemove(int id, out TicketRecord? removedTicket);
    
    bool TryUpdate(int id,TicketRecord ticket, out TicketRecord? updatedTicket);
}