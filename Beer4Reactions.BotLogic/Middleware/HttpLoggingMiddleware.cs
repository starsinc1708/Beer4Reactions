using System.Diagnostics;

namespace Beer4Reactions.BotLogic.Middleware;

public class HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            await next(context);
            
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;

            logger.LogInformation("HTTP | {Method} {Path} | {StatusCode} | {Duration}ms | IP[{RemoteIp}]",
                method, path, statusCode, duration, remoteIp);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;

            logger.LogError("HTTP | {Method} {Path} | ERROR | {Duration}ms | IP[{RemoteIp}] | Exception: {Exception}",
                method, path, duration, remoteIp, ex.Message);
            
            throw;
        }
    }
}
