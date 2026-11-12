using HelpDesk.Core.Interfaces;
using HelpDesk.Core.Records;

namespace HelpDesk.Tests.Models;

public class ThrowingTicketRepository : ITicketRepository
{
    public IEnumerable<TicketRecord> GetAll()
    {
        throw new Exception("Secret database details");
    }

    public bool TryGet(int id, out TicketRecord? ticket)
    {
        throw new Exception("Secret database details");
    }

    public bool TryAdd(TicketRecord addedTicket)
    {
        throw new Exception("Secret database details");
    }

    public bool TryUpdate(int id, TicketRecord ticket, out TicketRecord? updatedTicket)
    {
        throw new Exception("Secret database details");
    }

    public bool TryRemove(int id, out TicketRecord? removedTicket)
    {
        throw new Exception("Secret database details");
    }
}