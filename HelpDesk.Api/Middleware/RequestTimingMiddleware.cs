using System.Diagnostics;

namespace HelpDesk.Api.Middleware;

internal class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        var statusCode = context.Response.StatusCode;

        Console.WriteLine(
            $"{context.Request.Method} {context.Request.Path} {statusCode} {stopwatch.ElapsedMilliseconds} ms");
    }
}