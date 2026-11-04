using Microsoft.AspNetCore.Mvc.Filters;

namespace HelpDesk.Api.Filters;

internal class AuditActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        Console.WriteLine($"==Before==\nAction: {context.ActionDescriptor.DisplayName} Time: {DateTime.UtcNow}");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        Console.WriteLine($"==After==\nAction: {context.ActionDescriptor.DisplayName} Time: {DateTime.UtcNow}");
    }
}