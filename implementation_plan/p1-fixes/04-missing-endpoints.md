# P1-04 — Missing Backend Endpoints

**Goal**: Add missing CRUD endpoints across services.

**Fixes**: MISSING.md #6.2, #6.3, #6.4, #6.5, #6.6

---

## 6.2 — Category Update/Delete (Catalog.API)

File: `src/Microservices/Catalog/Catalog.API/Endpoints/CategoryEndpoints.cs`

Add:
```csharp
group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryCommand cmd, ISender sender, CancellationToken ct) => { ... });
group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => { ... });
```

Requires: `UpdateCategoryCommand`, `DeleteCategoryCommand` + handlers in Application layer.

## 6.3 — Inventory List Endpoint

File: `src/Microservices/Inventory/Inventory.API/Endpoints/InventoryEndpoints.cs`

Add:
```csharp
group.MapGet("/", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ListInventoryItemsQuery(), ct);
    return Results.Ok(result.Value);
})
.RequireAuthorization();
```

Requires: `ListInventoryItemsQuery` + handler.

## 6.4 — Payment Refund Endpoint

File: `src/Microservices/Payment/Payment.API/Endpoints/PaymentEndpoints.cs`

Add:
```csharp
group.MapPost("/{orderId:guid}/refund", async (Guid orderId, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new RefundPaymentCommand(orderId), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
})
.RequireAuthorization("Admin");
```

Requires: `RefundPaymentCommand` + handler. Mock gateway returns success.

## 6.5 — Media List Endpoint

File: `src/Microservices/Media/Media.API/Endpoints/MediaEndpoints.cs`

Add:
```csharp
group.MapGet("/", async ([FromServices] BlobServiceClient blobClient, CancellationToken ct) =>
{
    var container = blobClient.GetBlobContainerClient("media");
    var items = new List<MediaItemResponse>();
    await foreach (var blob in container.GetBlobsAsync(cancellationToken: ct))
    {
        items.Add(new MediaItemResponse(blob.Name, blob.Properties.ContentLength ?? 0, blob.Properties.ContentType));
    }
    return Results.Ok(items);
})
.RequireAuthorization();
```

## 6.6 — Change Password Endpoint

File: `src/Microservices/Identity/Identity.API/Endpoints/AuthEndpoints.cs`

Add:
```csharp
group.MapPost("/change-password", async (
    ChangePasswordCommand command,
    ISender sender,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var cmd = command with { UserId = userId! };
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
})
.RequireAuthorization();
```

Requires: `ChangePasswordCommand` + handler that verifies old password and sets new one.

## Done When
- [ ] Category PUT/DELETE endpoints
- [ ] Inventory GET list endpoint
- [ ] Payment POST refund endpoint
- [ ] Media GET list endpoint
- [ ] Identity POST change-password endpoint
- [ ] All endpoints have proper authorization
