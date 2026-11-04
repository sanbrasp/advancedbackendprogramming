namespace HelpDesk.Api.Middleware;

internal class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    
    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.ContainsKey("X-API-Key"))
        {
            httpContext.Response.StatusCode = 401;
            return;
        }
        await _next.Invoke(httpContext);
    }
}