namespace HelpDesk.Api.Middleware;

public static class RequestTimingMiddlewareExtension
{
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimingMiddleware>();
    }
}