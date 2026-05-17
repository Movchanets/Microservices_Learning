# Plan 14: SignalR Hub Authentication

## Goal
Add JWT Bearer authentication to Notification.Worker so only authenticated users can connect to the SignalR hub, and `Clients.User(buyerId)` correctly targets the authenticated user.

## Context
- **Current state:** `NotificationHub` has no `[Authorize]` attribute. `Notification.Worker/Program.cs` has no `UseAuthentication()`/`UseAuthorization()`. Anyone can connect with any `buyerId` query string parameter. `BuyerIdUserIdProvider` trusts the query string blindly.
- **Target state:** WebSocket handshake validates JWT token. Only authenticated users connect. `BuyerIdUserIdProvider` uses the authenticated user's claims (not untrusted query string). `Clients.User(buyerId)` correctly maps to the authenticated user.
- **Root cause:** Notification.Worker was built as a minimal worker without auth middleware.

## Prerequisites
- Identity.API issues JWT tokens — exists
- Other services use `AddAuthentication().AddJwtBearer()` — exists pattern
- `NotificationService` (frontend) uses `withCredentials: true` — exists
- YARP BFF forwards cookies as Bearer tokens — exists

## Backend Changes

### 1. Add JWT Authentication to Notification.Worker
**File:** `src/Microservices/Notification/Notification.Worker/Program.cs`

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Identity:Issuer"],
        };

        // SignalR sends token via query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
```

Add to pipeline:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### 2. Add [Authorize] to NotificationHub
**File:** `src/Microservices/Notification/Notification.Worker/Hubs/NotificationHub.cs`

```csharp
[Authorize]
public sealed class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        var buyerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        logger.LogInformation("Client connected: ConnectionId={ConnectionId}, BuyerId={BuyerId}",
            Context.ConnectionId, buyerId ?? "anonymous");
        return base.OnConnectedAsync();
    }
    // ...
}
```

### 3. Update BuyerIdUserIdProvider to Use Claims
**File:** `src/Microservices/Notification/Notification.Worker/Hubs/UserIdProvider.cs`

```csharp
public sealed class BuyerIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Prefer authenticated claims over query string
        var claimId = connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claimId))
            return claimId;

        // Fallback for backward compatibility (deprecated)
        var httpContext = connection.GetHttpContext();
        return httpContext?.Request.Query["buyerId"].ToString();
    }
}
```

### 4. Add Identity Configuration to appsettings
**File:** `src/Microservices/Notification/Notification.Worker/appsettings.json`

```json
{
  "Identity": {
    "Authority": "https://localhost:5001",
    "Issuer": "Marketplace.Identity"
  }
}
```

### 5. Update Frontend NotificationService to Send Token
**File:** `src/web/src/app/core/signalr/notification.service.ts`

```typescript
withUrl(`/hubs/notifications?buyerId=${encodeURIComponent(buyerId)}`, {
  transport: HttpTransportType.WebSockets,
  accessTokenFactory: () => getStoredToken(),  // JWT from cookie/session
})
```

Note: Since the BFF uses cookie auth, the frontend may need to extract the JWT from the cookie or use a dedicated token endpoint. Alternatively, the BFF can inject the token when proxying WebSocket connections.

### 6. Update YARP Configuration for WebSocket Auth
**File:** `src/Gateways/ApiGateway/appsettings.json`

Ensure YARP forwards the JWT token for WebSocket connections to Notification.Worker. This may require the BFF to inject the Bearer token from the cookie session.

## E2E Verification

### Spec File: `tests/E2ETests/tests/signalr-auth.spec.ts`

**Scenario:** Unauthenticated WebSocket is rejected. Authenticated buyer receives order updates.

```
TEST: signalr-auth.spec.ts

Setup:
  1. Register buyer via API, get JWT token

Test: "unauthenticated websocket connection is rejected"
  2. Attempt SignalR connection WITHOUT token
  3. Verify connection fails with 401 Unauthorized

Test: "authenticated buyer receives order update via SignalR"
  4. Login as buyer in browser (establishes authenticated session)
  5. Verify SignalR connected (notification.service.connected === true)
  6. Create store + product via seller API
  7. Add product to cart via API
  8. Navigate to /cart → checkout → place order
  9. Wait for order completion
  10. Verify SignalR received OrderUpdate message
  11. Verify notification toast or order status update appears

Test: "buyer only receives own order updates"
  12. Register second buyer via API
  13. Login as second buyer in different browser context
  14. First buyer places order
  15. Verify second buyer does NOT receive the order update
```

### New Page Objects
- None — uses existing pages + SignalR service signals

### Files to Create/Modify
```
tests/E2ETests/tests/signalr-auth.spec.ts                  # NEW
```

## Acceptance Criteria
- [ ] Notification.Worker has JWT authentication configured
- [ ] `NotificationHub` has `[Authorize]` attribute
- [ ] `BuyerIdUserIdProvider` prefers claims over query string
- [ ] Unauthenticated WebSocket connections are rejected (401)
- [ ] Authenticated buyers receive their own order updates
- [ ] Buyers don't receive other buyers' updates
- [ ] E2E test passes: unauth rejected, auth receives updates, isolation works
- [ ] All existing tests still pass

## Verification Commands
```bash
dotnet build Marketplace.slnx
dotnet test tests/UnitTests/Notification.UnitTests/ --no-build
npx playwright test tests/E2ETests/tests/signalr-auth.spec.ts
```
