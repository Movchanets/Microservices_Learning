# Media.API — Research Findings & Architectural Decisions

## 1. Architecture: Single-Project Thin Service

**Decision:** Single project with folder-based layer separation (Domain/, Application/, Infrastructure/).

**Rationale:**
- AGENTS.md designates Media as "thin" (may skip Domain/Application layers)
- No complex domain invariants — entities are data containers
- MediatR provides CQRS without physical project separation
- Single Dockerfile, simpler deployment

**Actual structure:**
```
Media.API/
├── Domain/ (Entities, Enums, Interfaces)
├── Application/ (Commands, Queries, DTOs, Interfaces)
├── Infrastructure/ (Persistence, Storage, DI)
├── Endpoints/
├── Services/ (ImageProcessingService — kept from stub)
└── Program.cs
```

---

## 2. Domain Model: Entities (not AggregateRoot)

**Decision:** `MediaItem` and `GalleryEntry` inherit `Entity`, NOT `AggregateRoot`.

**Consequence:** DomainEventDispatcherInterceptor won't dispatch events on these entities. Integration events must be published directly from command handlers via `IPublishEndpoint.Publish()` after `SaveChangesAsync()`.

**Trade-off:** No outbox guarantee for integration events (published after commit, not during). Acceptable for a thin service — if publish fails, the gallery update is lost but the core operation succeeded. Retry logic can be added later if needed.

---

## 3. Integration Events: Direct Publishing

**Pattern:**
```csharp
// In handler — after SaveChanges
await publishEndpoint.Publish(new MediaUploadedIntegrationEvent(...), ct);
```

**NOT the pattern used in Catalog (domain event → handler → integration event):**
```csharp
// Catalog pattern — domain event dispatched by interceptor during SaveChanges
product.AddDomainEvent(new ProductCreatedDomainEvent(...));
// → DomainEventDispatcherInterceptor dispatches
// → ProductCreatedDomainEventHandler publishes ProductCreatedEvent via IPublishEndpoint
```

**Why different:** Media entities don't inherit AggregateRoot, so no domain events collection.

---

## 4. Catalog Consumers for Media Events

Three consumers in `Catalog.Infrastructure/Messaging/Consumers/`:

| Consumer | Event | Action |
|----------|-------|--------|
| `MediaUploadedConsumer` | `MediaUploadedIntegrationEvent` | Updates Product.ImageUrl / Sku.ImageUrl when IsPrimary=true |
| `GalleryUpdatedConsumer` | `GalleryUpdatedIntegrationEvent` | Updates ImageUrl from primary gallery item |
| `MediaDeletedConsumer` | `MediaDeletedIntegrationEvent` | Clears ImageUrl (GalleryUpdatedConsumer will re-set if new primary) |

**Design choice:** MediaDeletedConsumer clears ImageUrl immediately. This is safe because GalleryUpdatedConsumer will fire shortly after with the new primary. Worst case: ImageUrl is temporarily null.

---

## 5. BFF Product Enrichment

**Pattern:** `ProductBffService` fetches product from catalog-api, then fetches gallery from media-api, merges into single response.

**Endpoints:**
- `/bff/catalog/products/{id}` — product + gallery (used by product detail page)
- `/bff/catalog/skus/{skuId}/gallery` — SKU-specific gallery

**Why BFF, not direct Media.API call from frontend:**
- Single HTTP call from frontend (reduces latency)
- BFF can cache/fallback gracefully
- Gallery data is coupled to product display

---

## 6. Frontend Gallery Component

**Component:** `ImageGalleryComponent` — main image + thumbnail strip.

**Behavior:**
- Shows primary image by default (from gallery)
- Falls back to `Product.ImageUrl` or `Sku.ImageUrl` if no gallery
- Clickable thumbnails to switch main image
- Supports gallery with 1 item (no thumbnail strip shown)

**Integration:**
- `CatalogService.getProduct()` calls `/bff/catalog/products/{id}` (BFF endpoint with gallery)
- Product interface extended with `gallery: GalleryItem[]`
- Seller form: file upload via `MediaService` (replaces URL text input)

---

## 7. Key Technical Decisions

| Decision | Alternative | Why |
|----------|-------------|-----|
| `AddDbContext` | `AddNpgsqlDbContext` | EF Core 10 conflict with AddDbContextPool |
| `SetKebabCaseEndpointNameFormatter()` | `("media", false)` | v8.5.9 no-arg overload only |
| Stream in command | IFormFile | Framework-agnostic application layer |
| ImageSharp kept | Remove thumbnails | Server-side thumbnails improve list page performance |
| Product.ImageUrl stays | Remove entirely | Denormalized cache for fast list views |
| Sku.ImageUrl added | Keep on Product only | Each variant can have own images (color, size) |

---

## 8. Known Issues & Future Work

| Issue | Status | Notes |
|-------|--------|-------|
| Search.UnitTests 1 failure | Pre-existing | Not related to Media changes |
| ImageSharp vulnerability | Warning | NU1902 — needs version bump |
| WithOpenApi deprecated | Warning | ASPDEPR002 — cosmetic, still functional |
| YARP multipart pass-through | Not tested | Need to verify file upload works through Gateway |
| Media handler unit tests | Not written | Phase 7 in task_plan |
| Catalog consumer unit tests | Not written | Future work |
