using System.Diagnostics;
using HelpDesk.Api.Interfaces;

namespace HelpDesk.Api.Middleware;

internal class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOperationSingleton _singletonOperation;

    public RequestTimingMiddleware(RequestDelegate next, IOperationSingleton singletonOperation)
    {
        _next = next;
        _singletonOperation = singletonOperation;
    }

    public async Task InvokeAsync(HttpContext context, IOperationScoped scopedOperation, IOperationTransient transientOperation)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        var statusCode = context.Response.StatusCode;

        Console.WriteLine(
            $"{context.Request.Method} {context.Request.Path} {statusCode} {stopwatch.ElapsedMilliseconds} ms\n" +
            $"Singleton: {_singletonOperation.OperationId}\nScoped: {scopedOperation.OperationId}\nTransient: {transientOperation.OperationId}\nTime: {stopwatch.ElapsedMilliseconds} ms");
    }
}