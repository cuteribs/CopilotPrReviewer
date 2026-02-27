// [MIDDLEWARE] Business logic in middleware
// [MIDDLEWARE] Missing next() call in some paths
// [MIDDLEWARE] Long-running operations blocking requests
// [CORR-3] Thread safety issues

namespace App.Middleware;

// [MIDDLEWARE] Business logic in middleware - should be in services/filters
public class BusinessLogicMiddleware
{
    private readonly RequestDelegate _next;

    // [CORR-3] Shared mutable state in middleware (singleton lifetime)
    private int _requestCount = 0;
    private Dictionary<string, int> _rateLimitTracker = new Dictionary<string, int>();

    public BusinessLogicMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // [CORR-3] Race condition on shared state
        _requestCount++;

        // [MIDDLEWARE] Business logic: rate limiting belongs in a filter/service
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // [CORR-3] Non-thread-safe dictionary operations
        if (_rateLimitTracker.ContainsKey(clientIp))
        {
            _rateLimitTracker[clientIp]++;
            if (_rateLimitTracker[clientIp] > 100)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Rate limited");
                // [MIDDLEWARE] Missing next() call - breaks pipeline for rate-limited requests
                // But this is intentional rate limiting... however still business logic in middleware
                return;
            }
        }
        else
        {
            _rateLimitTracker[clientIp] = 1;
        }

        // [MIDDLEWARE] Business logic: discount calculation in middleware!
        if (context.Request.Path.StartsWithSegments("/api/order-management"))
        {
            // [MIDDLEWARE] Long-running operation blocking requests
            await Task.Delay(100); // Simulating "processing"

            // [MIDDLEWARE] Request body read - could break downstream reading
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // [MIDDLEWARE] Business logic in middleware
            if (body.Contains("\"amount\"") && body.Contains("999"))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Suspicious amount detected");
                // [MIDDLEWARE] Missing next() call again
                return;
            }
        }

        // [SEC-4] Logging all request headers (may contain auth tokens)
        foreach (var header in context.Request.Headers)
        {
            Console.WriteLine($"Header: {header.Key} = {header.Value}");
        }

        // [LOG] Logging request/response at wrong level
        Console.WriteLine($"[DEBUG] Request #{_requestCount}: {context.Request.Method} {context.Request.Path}");

        await _next(context);

        // [LOG] Excessive logging
        Console.WriteLine($"[DEBUG] Response: {context.Response.StatusCode}");
    }
}
