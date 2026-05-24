# P2-05 — Email, Verification & Alerts

**Goal**: Add email sending, email verification on registration, and low-stock alerts.

**Fixes**: MISSING.md #10.2, #10.3, #10.5

---

## Email Service

### Abstraction

File: `src/BuildingBlocks/Infrastructure/Services/IEmailService.cs`
```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
```

### Implementation (SendGrid or SMTP)

File: `src/Microservices/Identity/Identity.Infrastructure/Services/SmtpEmailService.cs`

Configure via `appsettings.json`:
```json
{
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "From": "noreply@marketplace.local"
  }
}
```

For dev, use Mailpit or smtp4dev as a local SMTP server (can add to AppHost).

## Email Verification Flow

1. On registration, generate verification token, save to user record
2. Send email with verification link: `/api/identity/auth/verify-email?token=...`
3. New endpoint `POST /api/identity/auth/verify-email` validates token, marks email as verified
4. Add `EmailVerified` bool to User aggregate

## Low-Stock Alerts

File: `src/Microservices/Inventory/Inventory.Infrastructure/Messaging/LowStockConsumer.cs`

When stock falls below threshold (e.g., 10), publish `LowStockEvent`:
```csharp
public record LowStockEvent(string Sku, int CurrentQuantity, int Threshold, DateTime Timestamp);
```

Notification.Worker consumes it and sends alert to admin users via SignalR.

## Done When
- [ ] IEmailService abstraction + SMTP implementation
- [ ] Email verification endpoint + flow
- [ ] Low-stock threshold check + event
- [ ] Notification worker sends low-stock alerts
