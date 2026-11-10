using System.Collections.Concurrent;
using HelpDesk.Core.Interfaces;
using HelpDesk.Core.Records;

namespace HelpDesk.Infrastructure.Data;

public class InMemoryTicketRepository : ITicketRepository
{
    private static readonly DateTime _dateOfDeadline = DateTime.UtcNow.AddDays(14);
    
    private readonly ConcurrentDictionary<int, TicketRecord> _records = new ()
    {
        [1] = new(1, "sandra", DateTime.UtcNow, _dateOfDeadline, "My system is bugged!")
    };
    
    public IEnumerable<TicketRecord> GetAll()
    {
        return _records.Values;
    }

    public bool TryGet(int id, out TicketRecord? ticket)
    {
        return _records.TryGetValue(id, out ticket);
    }

    public bool TryAdd(TicketRecord addedTicket)
    {
        return _records.TryAdd(addedTicket.Id, addedTicket);
    }

    public bool TryRemove(int id, out TicketRecord? removedTicket)
    {
        return _records.TryRemove(id, out removedTicket);
    }

    public bool TryUpdate(int id, TicketRecord ticket, out TicketRecord? updatedTicket)
    {
        var exists = _records.TryGetValue(id, out var existing);
        if (!exists)
        {
            updatedTicket = null;
            return false;
        }

        updatedTicket = ticket with { Id = id };
        
        var success = existing != null && _records.TryUpdate(id, updatedTicket, existing);
        if (!success)
        {
            updatedTicket = null;
        }
        
        return success;
    }
}