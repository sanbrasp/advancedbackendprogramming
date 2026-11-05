using HelpDesk.Api.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IOperationTransient _transient;
    private readonly IOperationScoped _scoped;
    private readonly IOperationSingleton _singleton;

    public DiagnosticsController(IOperationTransient transient, IOperationScoped scoped, IOperationSingleton singleton)
    {
        _transient = transient;
        _scoped = scoped;
        _singleton = singleton;
    }

    [HttpGet]
    public IActionResult GetOperationId()
    {
        return Ok(new
        {
            _transient = _transient.OperationId, _scoped = _scoped.OperationId, _singleton = _singleton.OperationId
        });
    }
}