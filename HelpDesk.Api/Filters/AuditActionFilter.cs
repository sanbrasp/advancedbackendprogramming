using Microsoft.AspNetCore.Mvc.Filters;

namespace HelpDesk.Api.Filters;

internal class AuditActionFilter : IActionFilter
{
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(ILogger<AuditActionFilter> logger)
    {
        _logger = logger;
    }
    
    public void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation("Action {Action} starting", context.ActionDescriptor.DisplayName);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation("Action {Action} finished", context.ActionDescriptor.DisplayName);
    }
}