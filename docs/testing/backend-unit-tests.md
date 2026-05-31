# Backend Unit Test Inventory

**Project:** Marketplace Microservices
**Framework:** xUnit + Moq + FluentAssertions
**Last Updated:** 2026-05-31
**Total:** 300 tests across 12 projects

---

## Test Projects

| Project | Path | ~Tests |
|---------|------|--------|
| Cart.UnitTests | `tests/UnitTests/Cart.UnitTests/` | 31 |
| Catalog.UnitTests | `tests/UnitTests/Catalog.UnitTests/` | 30 |
| Identity.UnitTests | `tests/UnitTests/Identity.UnitTests/` | 45 |
| Inventory.UnitTests | `tests/UnitTests/Inventory.UnitTests/` | 8 |
| **Media.UnitTests** | `tests/UnitTests/Media.UnitTests/` | **11** |
| Notification.UnitTests | `tests/UnitTests/Notification.UnitTests/` | 20 |
| Ordering.UnitTests | `tests/UnitTests/Ordering.UnitTests/` | 69 |
| Payment.UnitTests | `tests/UnitTests/Payment.UnitTests/` | 30 |
| Search.UnitTests | `tests/UnitTests/Search.UnitTests/` | 4 |
| StoreManagement.UnitTests | `tests/UnitTests/StoreManagement.UnitTests/` | 29 |
| ApiGateway.UnitTests | `tests/UnitTests/ApiGateway.UnitTests/` | 7 |
| BuildingBlocks.Infrastructure.UnitTests | `tests/UnitTests/BuildingBlocks.Infrastructure.UnitTests/` | 16 |

---

## Media.UnitTests (11 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Application/UploadMediaHandlerTests.cs` | Application | Valid upload, invalid content type, file too large |
| `Application/DeleteMediaHandlerTests.cs` | Application | Delete existing media, not found |
| `Application/GetGalleryHandlerTests.cs` | Application | Gallery with entries, empty gallery |
| `Application/SetPrimaryMediaHandlerTests.cs` | Application | Set primary valid, not in gallery |
| `Application/UpdateGalleryOrderHandlerTests.cs` | Application | Reorder valid, no entries |

---

## Catalog.UnitTests — Media Consumer Tests (7 tests)

| Test File | Layer | What It Tests |
|-----------|-------|---------------|
| `Infrastructure/MediaUploadedConsumerTests.cs` | Consumer | Primary product upload, primary SKU upload, non-primary ignored |
| `Infrastructure/GalleryUpdatedConsumerTests.cs` | Consumer | Gallery with primary, no primary |
| `Infrastructure/MediaDeletedConsumerTests.cs` | Consumer | Product image clear, SKU image clear |

---

## How to Run

```bash
# All backend unit tests
dotnet test tests/UnitTests/ --verbosity normal

# Single service
dotnet test tests/UnitTests/Media.UnitTests/ --verbosity normal

# With coverage
dotnet test tests/UnitTests/ --collect:"XPlat Code Coverage"
```

---

*Last verified: 2026-05-31. Search.UnitTests has 1 pre-existing failure (not related to Media changes).*
