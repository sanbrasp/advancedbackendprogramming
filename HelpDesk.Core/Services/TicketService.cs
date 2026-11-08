using HelpDesk.Core.Interfaces;
using HelpDesk.Core.Records;

namespace HelpDesk.Core.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepo;

    public TicketService(ITicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }
    
    
    public IEnumerable<TicketRecord> GetAll()
    {
        return _ticketRepo.GetAll();
    }

    public bool TryGet(int id, out TicketRecord? ticket)
    {
        return _ticketRepo.TryGet(id, out ticket);
    }

    public bool TryAdd(TicketRecord addedTicket)
    {
        return _ticketRepo.TryAdd(addedTicket);
    }

    public bool TryUpdate(int id, TicketRecord ticket, out TicketRecord? updatedTicket)
    {
        return _ticketRepo.TryUpdate(id, ticket, out updatedTicket);
    }

    public bool TryRemove(int id, out TicketRecord? removedTicket)
    {
        return _ticketRepo.TryRemove(id, out removedTicket);
    }
}