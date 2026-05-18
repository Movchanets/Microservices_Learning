# Bug Report: Cart AddItem — JSON Cycle + DbUpdateConcurrencyException

**Date**: 2026-05-18
**Service**: Cart API
**Endpoint**: `POST /api/cart/items`
**Severity**: High — blocks all add-to-cart operations

---

## Summary

Two distinct bugs triggered by the same `AddCartItemCommand` flow:

1. **JsonException** — Object cycle during response serialization (`ShoppingCart.Items.Cart.Items.Cart...` infinite loop)
2. **DbUpdateConcurrencyException** — UPDATE on `CartItems` affects 0 rows after successful INSERT

Both errors cause the endpoint to return 500.

---

## Error 1: JSON Serialization Object Cycle

### Error Message

```
System.Text.Json.JsonException: A possible object cycle was detected.
This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64.
Path: $.Items.Cart.Items.Cart.Items.Cart.Items.Cart.Items.Cart...
```

### Root Cause

`CartItem` has a back-reference navigation property to its parent:

```csharp
// Cart.Domain/Aggregates/CartItem.cs:14
public ShoppingCart Cart { get; private set; } = null!;
```

When the endpoint returns the result:

```csharp
// Cart.API/Endpoints/CartEndpoints.cs:84
return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
```

`Results.Ok(result.Value)` uses the **default System.Text.Json serializer** which does NOT have the `ShoppingCartJsonConverter` registered. The default serializer follows the object graph:

```
Result<ShoppingCart>.Value (ShoppingCart)
  → Items (List<CartItem>)
    → Cart (ShoppingCart)          ← navigation back to parent
      → Items (List<CartItem>)
        → Cart (ShoppingCart)      ← CYCLE
          → ... (infinite)
```

The `ShoppingCartJsonConverter` in `Cart.Infrastructure/Serialization/ShoppingCartJsonConverter.cs` correctly handles this by only writing `BuyerId` + `Items` (without the `Cart` back-reference), BUT it is only registered in `CartRepository` for Redis cache serialization — NOT in the API's JSON pipeline.

### Evidence

- `Cart.Infrastructure/Repositories/CartRepository.cs:12-15` — converter only used for cache
- `Cart.API/Program.cs` — no global JSON converter registration
- `Cart.API/Endpoints/CartEndpoints.cs:84` — `Results.Ok()` uses default serializer

### Fix

Register the `ShoppingCartJsonConverter` globally in `Program.cs`:

```csharp
// Cart.API/Program.cs — add after builder creation
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new ShoppingCartJsonConverter());
});
```

OR remove the navigation property from `CartItem` (preferred for DDD — aggregates shouldn't have back-references):

```csharp
// Cart.Domain/Aggregates/CartItem.cs — REMOVE line 14:
// public ShoppingCart Cart { get; private set; } = null!;

// Cart.Infrastructure/Data/CartDbContext.cs — REMOVE line 21:
// .WithOne(i => i.Cart)
// Replace with:
.WithOne()
```

### Why DDD prefers removing the back-reference

In DDD, the aggregate root (`ShoppingCart`) owns its children (`CartItem`). Children should not navigate back to the parent — that's an ORM artifact, not a domain concept. The `CartId` foreign key is sufficient for EF Core to manage the relationship.

---

## Error 2: DbUpdateConcurrencyException (0 rows affected on UPDATE)

### Error Message

```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
The database operation was expected to affect 1 row(s), but actually affected 0 row(s);
data may have been modified or deleted since entities were loaded.
```

### Flow Analysis

From the logs, two sequential requests hit the endpoint:

**Request A** (first add — cart doesn't exist):

```
1. SELECT from ProductPrices (get price for SKU)           → OK
2. SELECT from ShoppingCarts JOIN CartItems (load cart)     → cart not found
3. INSERT into ShoppingCarts + CartItems                    → SUCCESS (new cart + item)
4. Return Result<ShoppingCart> to endpoint                  → JsonException during serialization
   ⚠️ DB commit already happened — cart + item ARE in the database
```

**Request B** (second add — same or different SKU):

```
1. SELECT from ProductPrices (get price for SKU)           → OK
2. SELECT from ShoppingCarts JOIN CartItems (load cart)     → cart FOUND (from Request A)
3. If SAME SKU: AddQuantity on existing item → UPDATE
   If DIFF SKU: new CartItem → INSERT
4. UPDATE CartItems SET ... WHERE Id = @p5                  → 0 ROWS AFFECTED ❌
```

### Root Cause Hypothesis

The `UPDATE` targets a CartItem `Id` that doesn't exist in the database. This happens because:

1. **Request A** inserts a CartItem with a new `Guid.NewGuid()` Id (from `Entity.cs:13`)
2. Request A's `SaveChangesAsync` commits the INSERT
3. Request A fails during JSON serialization (Error 1), but the DbContext is disposed
4. **Request B** creates a NEW scoped DbContext and loads the cart
5. The cart is loaded with its items from DB — the CartItem should have the DB's Id

**Most likely cause**: The `CartItem` loaded in Request B has the correct DB Id, but the `ShoppingCart` aggregate is modifying the `CartItem` in a way that changes its tracked state. Specifically, if `AddItem` is called with the same SKU:

```csharp
// ShoppingCart.cs:27-31
var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
if (existingItem != null)
{
    existingItem.AddQuantity(quantity);  // modifies tracked entity
}
```

This SHOULD work. But if there's a **concurrency issue** (two simultaneous requests both loading the same cart before either saves), the second save will fail because the first already committed.

**Alternative cause**: The `CartId` column in the UPDATE is being set to a different value than expected. Looking at the SQL:

```sql
UPDATE "CartItems" SET "CartId" = @p0, ... WHERE "Id" = @p5;
```

The `@p0` is the CartId. If the CartItem was loaded from a cart with `BuyerId = "user1"` but the tracked ShoppingCart has a different state, the FK might not match.

### Evidence

- `CartRepository.cs:50-63` — `GetOrCreateTrackedCartAsync` loads tracked entities
- `AddCartItemCommand.cs:20-23` — calls GetOrCreate, modifies, saves
- No concurrency handling (no version token, no retry, no distributed lock)
- The ShoppingCart `Id` (Guid) is different from `BuyerId` (string) — potential confusion in FK mapping

### Fix Options

**Option A: Add optimistic concurrency with version token** (recommended)

```csharp
// ShoppingCart.cs — add version property
public int Version { get; private set; }

// CartDbContext.cs — configure as concurrency token
builder.Entity<ShoppingCart>(b =>
{
    b.Property(x => x.Version).IsConcurrencyToken();
});
```

**Option B: Use upsert pattern for CartItems**
Instead of relying on EF Core change tracking, explicitly handle the "insert or update" logic:

```csharp
// In AddItem or the handler — check if item exists in DB before adding
var existingItem = cart.Items.FirstOrDefault(i => i.Sku == sku);
if (existingItem != null)
{
    existingItem.AddQuantity(quantity);
    // EF Core will generate UPDATE — but verify the item exists in DB first
}
else
{
    cart.AddItem(sku, quantity, price, sellerId);
    // EF Core will generate INSERT
}
```

**Option C: Reload after failed save (retry pattern)**

```csharp
// In SaveCartAsync — catch concurrency exception and retry
public async Task SaveCartAsync(ShoppingCart cart, CancellationToken ct)
{
    try
    {
        await dbContext.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        // Reload and retry
        await dbContext.Entry(cart).ReloadAsync(ct);
        await dbContext.SaveChangesAsync(ct);
    }
    await UpdateCacheAsync(cart, ct);
}
```

---

## Related Files

| File                      | Path                                                                                    | Role                                                 |
| :------------------------ | :-------------------------------------------------------------------------------------- | :--------------------------------------------------- |
| ShoppingCart aggregate    | `src/Microservices/Cart/Cart.Domain/Aggregates/ShoppingCart.cs`                         | Root entity with Items collection                    |
| CartItem entity           | `src/Microservices/Cart/Cart.Domain/Aggregates/CartItem.cs`                             | Child entity with `Cart` back-reference (BUG SOURCE) |
| CartDbContext             | `src/Microservices/Cart/Cart.Infrastructure/Data/CartDbContext.cs`                      | EF Core config, FK mapping                           |
| CartRepository            | `src/Microservices/Cart/Cart.Infrastructure/Repositories/CartRepository.cs`             | GetOrCreateTrackedCartAsync, SaveCartAsync           |
| AddCartItemCommand        | `src/Microservices/Cart/Cart.Application/Commands/AddCartItemCommand.cs`                | Handler that triggers both bugs                      |
| CartEndpoints             | `src/Microservices/Cart/Cart.API/Endpoints/CartEndpoints.cs`                            | POST /api/cart/items endpoint (line 75-85)           |
| Program.cs                | `src/Microservices/Cart/Cart.API/Program.cs`                                            | Missing global JSON converter                        |
| ShoppingCartJsonConverter | `src/Microservices/Cart/Cart.Infrastructure/Serialization/ShoppingCartJsonConverter.cs` | Correct serializer, not registered globally          |
| AggregateRoot base        | `src/BuildingBlocks/SharedContracts/Abstractions/AggregateRoot.cs`                      | Base class (no version/concurrency)                  |
| Entity base               | `src/BuildingBlocks/SharedContracts/Abstractions/Entity.cs`                             | Id = Guid.NewGuid()                                  |
| Result<T>                 | `src/BuildingBlocks/Infrastructure/Models/Result.cs`                                    | Return type wrapping ShoppingCart                    |
| GlobalExceptionMiddleware | `src/BuildingBlocks/Infrastructure/Middleware/GlobalExceptionMiddleware.cs`             | Catches and logs both exceptions                     |

---

## DB Schema (from CartDbContext.OnModelCreating)

```
ShoppingCarts
  PK: BuyerId (string)
  Id: Guid (from Entity base — NOT the PK!)

CartItems
  PK: Id (Guid, from Entity base)
  FK: CartId → ShoppingCarts.BuyerId (string)
  Unique Index: (CartId, Sku)

ProductPrices
  PK: Id
  Sku, Price, Currency, Name, UpdatedAt
```

---

## Recommended Fix Priority

1. **Fix Error 1 first** (JSON cycle) — it's a 1-line fix and it's what causes Request A to fail after DB commit, which sets up Error 2
2. **Fix Error 2** (concurrency) — add version token or retry logic
3. **Consider removing `CartItem.Cart` navigation property** — eliminates the cycle at the model level and is the DDD-correct approach

---

## Testing Approach

1. Add item to empty cart → should return 200 with cart JSON (no cycle)
2. Add same item again → should return 200 with updated quantity
3. Add different item → should return 200 with both items
4. Concurrent add requests → should not throw 500

## Prevention

- Register all custom JSON converters globally in Program.cs, not just in repositories
- Remove ORM navigation properties that create circular references in DDD aggregates
- Add concurrency tokens to aggregate roots that support concurrent access
- Integration tests should cover the full request/response cycle, not just handler logic

         AddItem_ConcurrentRequests_NoConcurrencyException
         AddItem_DifferentSkus_KeepsBothItems

  Did not expect any exception, but found Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the in...

Xunit.Sdk.XunitException
Did not expect any exception, but found Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
---> Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "PK_ShoppingCarts"

DETAIL: Detail redacted as it may contain sensitive data. Specify 'Include Error Detail' in the connection string to include this information.
at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
at Npgsql.NpgsqlCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
Exception data:
Severity: ERROR
SqlState: 23505
MessageText: duplicate key value violates unique constraint "PK*ShoppingCarts"
Detail: Detail redacted as it may contain sensitive data. Specify 'Include Error Detail' in the connection string to include this information.
SchemaName: public
TableName: ShoppingCarts
ConstraintName: PK_ShoppingCarts
File: nbtinsert.c
Line: 666
Routine: \_bt_check_unique
--- End of inner exception stack trace ---
at Microsoft.EntityFrameworkCore.Update.ReaderModificationCommandBatch.ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.Update.Internal.BatchExecutor.ExecuteAsync(IEnumerable`1 commandBatches, IRelationalConnection connection, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Storage.RelationalDatabase.SaveChangesAsync(IList`1 entries, CancellationToken cancellationToken)
at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(IList`1 entriesToSave, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.ChangeTracking.Internal.StateManager.SaveChangesAsync(StateManager stateManager, Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.NpgsqlExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)
   at Cart.Infrastructure.Repositories.CartRepository.SaveCartAsync(ShoppingCart cart, CancellationToken ct) in D:\code\Microservices\src\Microservices\Cart\Cart.Infrastructure\Repositories\CartRepository.cs:line 83
   at Cart.IntegrationTests.CartRepositoryTests.<>c__DisplayClass7_0.<<AddItem_ConcurrentRequests_NoConcurrencyException>b__0>d.MoveNext() in D:\code\Microservices\tests\IntegrationTests\Cart.IntegrationTests\CartRepositoryTests.cs:line 202
--- End of stack trace from previous location ---
   at FluentAssertions.Specialized.NonGenericAsyncFunctionAssertions.NotThrowAsync(String because, Object[] becauseArgs).
   at FluentAssertions.Specialized.DelegateAssertionsBase`2.NotThrowInternal(Exception exception, String because, Object[] becauseArgs)
at FluentAssertions.Specialized.NonGenericAsyncFunctionAssertions.NotThrowAsync(String because, Object[] becauseArgs)
at Cart.IntegrationTests.CartRepositoryTests.AddItem_ConcurrentRequests_NoConcurrencyException() in D:\code\Microservices\tests\IntegrationTests\Cart.IntegrationTests\CartRepositoryTests.cs:line 207
at Xunit.Sdk.TestInvoker`1.<>c**DisplayClass47_0.<<InvokeTestMethodAsync>b**1>d.MoveNext() in /*/src/xunit.execution/Sdk/Frameworks/Runners/TestInvoker.cs:line 259
--- End of stack trace from previous location ---
at Xunit.Sdk.ExecutionTimer.AggregateAsync(Func`1 asyncAction) in /_/src/xunit.execution/Sdk/Frameworks/ExecutionTimer.cs:line 48
   at Xunit.Sdk.ExceptionAggregator.RunAsync(Func`1 code) in /\_/src/xunit.core/Sdk/ExceptionAggregator.cs:line 90
same exception type
