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
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }


    [HttpGet]
    public ActionResult<IEnumerable<TicketRecord>> GetAll()
    {
        return Ok(_ticketService.GetAll());
    }

    [HttpGet("{id}")]
    [ServiceFilter(typeof(ValidateIdFilter))]
    public ActionResult<TicketRecord> GetById(int id)
    {
        if (!_ticketService.TryGet(id, out var ticket))
        {
            return NotFound();
        }
        
        return Ok(ticket);
    }

    [HttpPost]
    public ActionResult<TicketRecord> Create(TicketRecord ticket)
    {
        if (!_ticketService.TryAdd(ticket))
        {
            return Conflict();
        }
        
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id}")]
    public ActionResult<TicketRecord> Update(int id, TicketRecord ticket)
    {
        if (!_ticketService.TryUpdate(id, ticket, out var updatedTicket))
        {
            return NotFound();
        }
        
        return Ok(updatedTicket);
    }

    [HttpDelete("{id}")]
    public ActionResult<TicketRecord> Delete(int id)
    {
        if (!_ticketService.TryRemove(id, out var removedTicket))
        {
            return NotFound();
        }
        
        return Ok(removedTicket);
    }
}