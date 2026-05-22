# Phase 13: Centralized Seeder Application (Seeder.App)

## Overview
The goal of this phase is to replace the distributed, database-level data seeding logic previously scattered across multiple microservices (Identity, StoreManagement, Catalog, Inventory) with a single, centralized `Seeder.App`.

The `Seeder.App` will act as an external client. It will read predefined, generated JSON data files and make HTTP requests to the centralized API Gateway (`API_URL`) to populate the system.

By decoupling seeding from the startup routines of individual microservices, we achieve:
1. **Clean Microservices:** Microservices no longer contain dev-only data logic or direct EF Core insertions bypassing domain behavior.
2. **Realistic Data Flow:** Seeded data exercises the entire stack (Gateway -> Controllers -> MediatR -> Domain -> DB -> Outbox -> Messaging -> Consumers), ensuring event-driven side effects work exactly as they would in production.
3. **Easier Maintenance:** Managing the state of seeded data is simpler when represented as clean JSON files rather than hardcoded C# arrays.

## Architecture

`Seeder.App` will be a standard .NET Worker Service or Console Application.

- **Trigger:** It will be registered in `AppHost` and run after the infrastructure and API Gateway are healthy.
- **Execution:** It iterates over JSON configuration, maps the data to HTTP request models, and sends POST/PUT requests to the API Gateway.
- **Completion:** Once all requests are successfully processed, the application exits successfully (Run-to-Completion).

## Execution Flow

The seeder will execute in a specific order to satisfy domain dependencies:

### 1. Identity Seeding
- **Action:** Read `users.json`.
- **Process:** Make HTTP POST requests to `POST /api/v1/auth/register` or equivalent admin endpoints.
- **Outcome:** Admin, Buyers, and Seller accounts are created.

### 2. Store Seeding
- **Action:** Read `stores.json`.
- **Process:** Authenticate as the generated Seller accounts to get JWTs.
- **Process:** Make HTTP POST requests to `POST /api/v1/stores`.
- **Outcome:** Stores are created, emitting integration events.

### 3. Catalog Seeding (Categories & Products)
- **Action:** Read `categories.json` and `products.json`.
- **Process:** Make HTTP POST requests to `POST /api/v1/catalog/categories` and `POST /api/v1/catalog/products`.
- **Outcome:** Categories and Products are established.

### 4. Event-Driven Propagation
- **Inventory & Pricing:** The seeder **does not** explicitly make requests to create inventory rows or base product prices unless setting initial stock levels is required via the API.
- **Reasoning:** When `Seeder.App` calls `POST /api/v1/catalog/products`, the Catalog service emits a `ProductCreatedDomainEvent`/`ProductCreatedIntegrationEvent`. The `Inventory` and `Cart` services will naturally consume this event to create `InventoryItem` (with a base quantity of 0) and `ProductPrice` read models respectively.

### 5. Optional: Stock Adjustments
- If the development environment requires non-zero stock, the seeder will wait briefly or poll until the inventory items are created via messaging, then make HTTP POST requests to `POST /api/v1/inventory/{sku}/stock` to add stock.

## Project Structure

```
src/
└── Tools/
    └── Seeder.App/
        ├── Program.cs
        ├── appsettings.json
        ├── Data/
        │   ├── users.json
        │   ├── stores.json
        │   ├── categories.json
        │   └── products.json
        ├── Services/
        │   ├── SeederService.cs
        │   └── ApiClient.cs
        └── Models/
            └── (DTOs representing JSON structure)
```

## Aspire Integration

In `AppHost.cs`, the seeder will be added and configured to run once the gateway is ready:

```csharp
var seederApp = builder.AddProject<Projects.Seeder_App>("seeder-app")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithEnvironment("ApiBaseUrl", gateway.GetEndpoint("http"));
```

## Next Steps
1. Create the `Seeder.App` project in the solution under a new `Tools` folder.
2. Generate realistic JSON datasets for the entities.
3. Implement `HttpClient` logic with authentication handlers (fetching and attaching JWTs).
4. Register the project in Aspire `AppHost` and configure startup dependencies.