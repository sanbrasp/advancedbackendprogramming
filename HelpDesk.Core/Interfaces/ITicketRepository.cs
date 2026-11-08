using HelpDesk.Core.Records;

namespace HelpDesk.Core.Interfaces;

public interface ITicketRepository
{
    IEnumerable<TicketRecord> GetAll();
    
    bool TryGet(int id, out TicketRecord? ticket);
    
    bool TryAdd(TicketRecord addedTicket);
    
    bool TryUpdate(int id,TicketRecord ticket, out TicketRecord? updatedTicket);
    
    bool TryRemove(int id, out TicketRecord? removedTicket);
}