using FluentValidation;
using HelpDesk.Api.DTOs;
using HelpDesk.Api.Filters;
using HelpDesk.Api.Interfaces;
using HelpDesk.Api.Settings;
using HelpDesk.Core;
using HelpDesk.Core.Interfaces;
using HelpDesk.Core.Records;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(AuditActionFilter))]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IOptions<TicketSettings> _ticketSettings;
    private readonly IValidator<CreateTicketRequestDto> _validator;

    public TicketsController(ITicketService ticketService, IOptions<TicketSettings> ticketSettings,
        IValidator<CreateTicketRequestDto> validator)
    {
        _ticketService = ticketService;
        _ticketSettings = ticketSettings;
        _validator = validator;
    }


    [HttpGet]
    public ActionResult<IEnumerable<TicketRecord>> GetAll()
    {
        var pageSize = _ticketSettings.Value.MaxTicketsReturned;
        {
            return Ok(_ticketService.GetAll().Take(pageSize));
        }
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
    public ActionResult<TicketRecord> Create(CreateTicketRequestDto ticketRequestDto)
    {
        var validationResult = _validator.Validate(ticketRequestDto);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var ticket = new TicketRecord(ticketRequestDto.Id, ticketRequestDto.User, ticketRequestDto.DateOfIssue,
            ticketRequestDto.DateOfDeadline,
            ticketRequestDto.Description);

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