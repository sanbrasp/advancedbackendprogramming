using HelpDesk.Api.Filters;
using HelpDesk.Api.Interfaces;
using HelpDesk.Api.Records;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(AuditActionFilter))]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _ticketsRepository;

    public TicketsController(ITicketRepository ticketsRepository)
    {
        _ticketsRepository = ticketsRepository;
    }


    [HttpGet]
    public ActionResult<IEnumerable<TicketRecord>> GetAll()
    {
        return Ok(_ticketsRepository.GetAll());
    }

    [HttpGet("{id}")]
    [ServiceFilter(typeof(ValidateIdFilter))]
    public ActionResult<TicketRecord> GetById(int id)
    {
        if (!_ticketsRepository.TryGet(id, out var ticket))
        {
            return NotFound();
        }
        
        return Ok(ticket);
    }

    [HttpPost]
    public ActionResult<TicketRecord> Create(TicketRecord ticket)
    {
        if (!_ticketsRepository.TryAdd(ticket))
        {
            return Conflict();
        }
        
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id}")]
    public ActionResult<TicketRecord> Update(int id, TicketRecord ticket)
    {
        if (!_ticketsRepository.TryUpdate(id, ticket, out var updatedTicket))
        {
            return NotFound();
        }
        
        return Ok(updatedTicket);
    }

    [HttpDelete("{id}")]
    public ActionResult<TicketRecord> Delete(int id)
    {
        if (!_ticketsRepository.TryRemove(id, out var removedTicket))
        {
            return NotFound();
        }
        
        return Ok(removedTicket);
    }
}