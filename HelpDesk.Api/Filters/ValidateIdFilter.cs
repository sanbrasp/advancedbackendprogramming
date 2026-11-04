using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HelpDesk.Api.Filters;

internal class ValidateIdFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("id", out var value) && value is int id && id <= 0)
        {
            context.Result = new BadRequestObjectResult("ID must be greater than 0");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        
    }
}