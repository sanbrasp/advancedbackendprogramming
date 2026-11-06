using HelpDesk.Api.Settings;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Middleware;

internal class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<SecuritySettings> _securitySettings;
    
    public ApiKeyMiddleware(RequestDelegate next, IOptions<SecuritySettings> securitySettings)
    {
        _next = next;
        _securitySettings = securitySettings;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue("X-API-Key", out var providedKey)
            || providedKey != _securitySettings.Value.ApiKey)
        {
            httpContext.Response.StatusCode = 401;
            return;
        }
        await _next.Invoke(httpContext);
    }
}