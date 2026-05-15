# P0-02 — Identity MassTransit + Store Verification Pipeline

**Goal**: Wire Identity.API to MassTransit so domain events become integration events. When admin verifies a store, the seller's role automatically updates to "Seller".

**Fixes**: MISSING.md #1.5, #4.1, #4.2

---

## Step 1: Add MassTransit to Identity.API

File: `src/Microservices/Identity/Identity.API/Program.cs`

Add after FluentValidation registration:
```csharp
// ── MassTransit v8 + Outbox ─────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
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

Add NuGet to `Identity.API.csproj`:
- `MassTransit.RabbitMQ` (match version 8.5.9)
- `MassTransit.EntityFrameworkCore` (match version 8.5.9)

## Step 2: Add integration events to SharedContracts

File: `src/BuildingBlocks/SharedContracts/Events/Identity/UserRegisteredEvent.cs`
```csharp
namespace BuildingBlocks.SharedContracts.Events.Identity;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime Timestamp);
```

File: `src/BuildingBlocks/SharedContracts/Events/Identity/UserRoleChangedEvent.cs`
```csharp
namespace BuildingBlocks.SharedContracts.Events.Identity;

public record UserRoleChangedEvent(
    Guid UserId,
    string OldRole,
    string NewRole,
    DateTime Timestamp);
```

File: `src/BuildingBlocks/SharedContracts/Events/StoreManagement/StoreVerifiedEvent.cs`
```csharp
namespace BuildingBlocks.SharedContracts.Events.StoreManagement;

public record StoreVerifiedEvent(
    Guid StoreId,
    string SellerId,
    DateTime Timestamp);
```

## Step 3: Publish domain events as integration events in Identity

Create a MediatR domain event handler that publishes to MassTransit:

File: `src/Microservices/Identity/Identity.Infrastructure/Messaging/UserRegisteredEventHandler.cs`
```csharp
public sealed class UserRegisteredEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(UserRegisteredDomainEvent notification, CancellationToken ct)
    {
        await publishEndpoint.Publish(new UserRegisteredEvent(
            notification.UserId,
            notification.Email,
            notification.FirstName,
            notification.LastName,
            notification.Role,
            DateTime.UtcNow), ct);
    }
}
```

## Step 4: Publish StoreVerifiedEvent from StoreManagement

Add a domain event handler in StoreManagement.Infrastructure:

File: `src/Microservices/StoreManagement/StoreManagement.Infrastructure/Messaging/StoreVerifiedEventHandler.cs`
```csharp
public sealed class StoreVerifiedEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<StoreVerifiedDomainEvent>
{
    public async Task Handle(StoreVerifiedDomainEvent notification, CancellationToken ct)
    {
        await publishEndpoint.Publish(new StoreVerifiedEvent(
            notification.StoreId,
            notification.SellerId,
            DateTime.UtcNow), ct);
    }
}
```

## Step 5: Add consumer in Identity to handle StoreVerifiedEvent

File: `src/Microservices/Identity/Identity.Infrastructure/Messaging/Consumers/StoreVerifiedConsumer.cs`
```csharp
public sealed class StoreVerifiedConsumer(
    IUserRepository userRepository,
    IUnitOfWork uow,
    ILogger<StoreVerifiedConsumer> logger)
    : IConsumer<StoreVerifiedEvent>
{
    public async Task Consume(ConsumeContext<StoreVerifiedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Store verified for seller {SellerId}, updating role", evt.SellerId);

        var user = await userRepository.GetByIdAsync(evt.SellerId, context.CancellationToken);
        if (user is null)
        {
            logger.LogWarning("User {SellerId} not found for role update", evt.SellerId);
            return;
        }

        user.ChangeRole(UserRole.Seller);
        await uow.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("User {SellerId} role updated to Seller", evt.SellerId);
    }
}
```

## Step 6: Wire IdentityDbContext with MassTransit Outbox

Ensure IdentityDbContext implements the Outbox entity types. Add to `OnModelCreating`:
```csharp
modelBuilder.AddInboxStateEntity();
modelBuilder.AddOutboxMessageEntity();
modelBuilder.AddOutboxStateEntity();
```

## Verification
- `dotnet build Marketplace.slnx`
- Verify Identity.API starts with MassTransit connected to RabbitMQ
- Verify StoreManagement publishes StoreVerifiedEvent when store is verified
- Verify Identity consumer receives event and updates user role

## Done When
- [ ] Identity.API has MassTransit + Outbox configured
- [ ] UserRegisteredEvent published on user registration
- [ ] StoreVerifiedEvent published on store verification
- [ ] Identity consumer updates user role to Seller on StoreVerifiedEvent
- [ ] All integration events in SharedContracts
- [ ] Solution builds clean
