# P2-04 — Rate Limiting & Request Logging

**Goal**: Add rate limiting and request/response logging to the API Gateway.

**Fixes**: MISSING.md #7.3, #10.1

---

## Rate Limiting

File: `src/Gateways/ApiGateway/Program.cs`

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// In pipeline:
app.UseRateLimiter();
```

Apply to routes in `appsettings.json` or via middleware.

## Request Logging Middleware

File: `src/Gateways/ApiGateway/Middleware/RequestLoggingMiddleware.cs`
```csharp
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("HTTP {Method} {Path} started",
            context.Request.Method, context.Request.Path);

        await next(context);

        sw.Stop();
        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
```

Register in Program.cs:
```csharp
app.UseMiddleware<RequestLoggingMiddleware>();
```

## Done When
- [ ] Rate limiter configured (100 req/min per IP)
- [ ] Request logging middleware logs method, path, status, duration
- [ ] 429 returned when rate limit exceeded
