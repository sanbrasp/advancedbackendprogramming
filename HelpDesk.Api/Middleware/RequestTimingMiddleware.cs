using System.Diagnostics;
using HelpDesk.Api.Interfaces;

namespace HelpDesk.Api.Middleware;

internal class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOperationSingleton _singletonOperation;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, IOperationSingleton singletonOperation,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _singletonOperation = singletonOperation;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOperationScoped scopedOperation,
        IOperationTransient transientOperation)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        var statusCode = context.Response.StatusCode;

        _logger.LogInformation("{Method} {Path} {ElapsedMs} ms",
            context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
        
        _logger.LogInformation("Operation IDs: Singleton {Singleton}, Scoped {Scoped}, Transient {Transient}",
            _singletonOperation.OperationId, scopedOperation.OperationId, transientOperation.OperationId);
    }
}