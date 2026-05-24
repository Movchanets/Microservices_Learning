# P1-05 — Gateway Improvements

**Goal**: Add token refresh and route-level auth policies to the API Gateway.

**Fixes**: MISSING.md #7.2, #7.4

---

## Token Refresh

File: `src/Gateways/ApiGateway/Middleware/TokenRefreshMiddleware.cs`

When the JWT is about to expire (e.g., within 5 minutes), automatically refresh it using the refresh token stored in the session cookie.

```csharp
public sealed class TokenRefreshMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if JWT is near expiry
        var expiresAt = context.Session.GetString("token_expires");
        if (DateTime.TryParse(expiresAt, out var expiry) && expiry < DateTime.UtcNow.AddMinutes(5))
        {
            // Call Identity.API refresh endpoint
            var refreshToken = context.Session.GetString("refresh_token");
            // ... refresh logic ...
        }
        await next(context);
    }
}
```

Register in Program.cs:
```csharp
app.UseMiddleware<TokenRefreshMiddleware>();
```

## Route-Level Auth Policies

File: `src/Gateways/ApiGateway/appsettings.json`

Add auth metadata to YARP routes:
```json
{
  "Routes": {
    "store-route": {
      "AuthorizationPolicy": "Seller",
      ...
    },
    "order-route": {
      "AuthorizationPolicy": "Authenticated",
      ...
    }
  }
}
```

File: `src/Gateways/ApiGateway/Program.cs`

Register authorization policies:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
    options.AddPolicy("Seller", p => p.RequireRole("Seller", "Admin"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
});
```

## Done When
- [ ] Token refresh middleware refreshes near-expiry JWTs
- [ ] YARP routes have authorization policies
- [ ] Unauthorized requests blocked at gateway level
