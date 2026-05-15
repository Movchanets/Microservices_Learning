# P0-04 — Cart Hardening

**Goal**: Add JWT Bearer auth to Cart.API and configure MassTransit Outbox for guaranteed event delivery.

**Fixes**: MISSING.md #2.3, #2.4

---

## Step 1: Add JWT Bearer auth to Cart.API

File: `src/Microservices/Cart/Cart.API/Program.cs`

Add JWT Bearer auth (same pattern as other services):
```csharp
// ── Authentication (JWT Bearer) ─────────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();
```

Add middleware:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

## Step 2: Extract buyerId from JWT claims

File: `src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs`

Replace `[FromHeader(Name = "x-buyer-id")] string buyerId` with `ClaimsPrincipal user`:
```csharp
group.MapGet("/", async (
    ClaimsPrincipal user,
    [FromServices] ISender sender,
    CancellationToken ct) =>
{
    var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(buyerId)) return Results.Unauthorized();
    var result = await sender.Send(new GetCartQuery(buyerId), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
})
.RequireAuthorization();
```

Apply same pattern to all cart endpoints (POST /, DELETE /, POST /checkout).

## Step 3: Configure MassTransit Outbox for Cart

File: `src/Microservices/Cart/Cart.API/Program.cs`

Currently Cart uses `AddMassTransit` with RabbitMQ but no Outbox. Add Outbox:
```csharp
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<CartDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("messaging"));
        cfg.ConfigureEndpoints(context);
    });
});
```

## Step 4: Update CartDbContext for Outbox

File: `src/Microservices/Cart/Cart.Infrastructure/Data/CartDbContext.cs`

Ensure the DbContext has Outbox entity configurations:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.AddInboxStateEntity();
    modelBuilder.AddOutboxMessageEntity();
    modelBuilder.AddOutboxStateEntity();
}
```

## Step 5: Add NuGet packages

File: `src/Microservices/Cart/Cart.Infrastructure/Cart.Infrastructure.csproj`

Add if missing:
- `MassTransit.EntityFrameworkCore` (8.5.9)
- `Microsoft.EntityFrameworkCore.Relational` (10.0.8)

## Verification
- `dotnet build Marketplace.slnx`
- Unauthenticated requests to /api/cart return 401
- Checkout event is published via Outbox (verify in logs)
- x-buyer-id header no longer accepted

## Done When
- [ ] Cart endpoints require JWT Bearer auth
- [ ] BuyerId extracted from JWT claims
- [ ] MassTransit Outbox configured for Cart
- [ ] CartDbContext has Outbox entity types
- [ ] Solution builds clean
