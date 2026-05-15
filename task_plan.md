# Phase 6 — StoreManagement.API & Media.API

**Goal**: Implement seller/store management (full Clean Architecture) and media storage (thin service with Azure Blob Storage).

**Depends on**: Phase 2 (Catalog)

**Status**: `complete`

---

## Phase Breakdown

### 6.1 — StoreManagement.API (Full Clean Architecture)
📄 Sub-Plan: `implementation_plan/phase-6/6.1-store-management.md`
- [x] 6.1.1 Scaffold 4 Clean Architecture projects (Domain, Application, Infrastructure, API)
- [x] 6.1.2 Domain layer — Store aggregate, SellerProfile entity, VerificationStatus enum, domain events
- [x] 6.1.3 Application layer — Commands (CreateStore, UpdateStore, VerifySeller), Queries (GetStore, ListStores), DTOs, Validators
- [x] 6.1.4 Infrastructure layer — StoreDbContext, EF Core configurations, repositories, ApplyMigrations pattern
- [x] 6.1.5 API layer — Minimal API endpoints, Program.cs, JWT auth, MediatR pipeline
- [x] 6.1.6 AppHost registration — storeDb, messaging, gateway wiring
- [x] 6.1.7 Unit tests — Domain logic (Store aggregate, verification flow) — 17 tests

### 6.2 — Media.API (Thin Service)
📄 Sub-Plan: `implementation_plan/phase-6/6.2-media-api.md`
- [x] 6.2.1 Create thin Media.API project
- [x] 6.2.2 Azure Blob Storage integration via Aspire.Azure.Storage.Blobs
- [x] 6.2.3 Implement upload, retrieve, delete endpoints
- [x] 6.2.4 Image processing — resize/thumbnail on upload (SixLabors.ImageSharp)
- [x] 6.2.5 AppHost registration — Azurite locally, Azure Storage in cloud
- [ ] 6.2.6 Integration tests — Upload → retrieve → verify (deferred to Phase 8)

### 6.3 — Gateway & Finalization
- [x] 6.3.1 Verify YARP routes (already configured in appsettings.json)
- [x] 6.3.2 Register both services in AppHost + Scalar
- [x] 6.3.3 Verify full solution builds — 0 errors

---

## Architecture Decisions

| Decision | Choice | Rationale |
|:---|:---|:---|
| StoreManagement architecture | Full Clean Architecture (4 layers) | Has business logic (seller verification, store lifecycle) |
| Media architecture | Thin service (API only) | CRUD wrapper around blob storage, no domain logic |
| Media storage | Azure Blob Storage via Aspire | Aspire provides Azurite emulator locally, Azure Storage in cloud |
| Image processing | SixLabors.ImageSharp | Cross-platform, no native dependencies, .NET 10 compatible |
| Database for StoreMgmt | PostgreSQL via Aspire (store-db) | Already defined in AppHost |
| Media database | None | Blob storage is the persistence layer |

---

## Files to Create/Modify

### New Files — StoreManagement
```
src/Microservices/StoreManagement/
├── StoreManagement.Domain/
│   ├── Aggregates/Store.cs
│   ├── Aggregates/IStoreRepository.cs
│   ├── Entities/SellerProfile.cs
│   ├── Enumerations/VerificationStatus.cs
│   └── Events/StoreCreatedDomainEvent.cs
├── StoreManagement.Application/
│   ├── Commands/CreateStore/CreateStoreCommand.cs
│   ├── Commands/CreateStore/CreateStoreHandler.cs
│   ├── Commands/CreateStore/CreateStoreValidator.cs
│   ├── Commands/UpdateStore/UpdateStoreCommand.cs
│   ├── Commands/UpdateStore/UpdateStoreHandler.cs
│   ├── Commands/VerifySeller/VerifySellerCommand.cs
│   ├── Commands/VerifySeller/VerifySellerHandler.cs
│   ├── Queries/GetStoreById/GetStoreByIdQuery.cs
│   ├── Queries/GetStoreById/GetStoreByIdHandler.cs
│   ├── Queries/ListStores/ListStoresQuery.cs
│   ├── Queries/ListStores/ListStoresHandler.cs
│   ├── DTOs/StoreDto.cs
│   └── DependencyInjection.cs
├── StoreManagement.Infrastructure/
│   ├── Persistence/StoreDbContext.cs
│   ├── Persistence/Configurations/StoreConfiguration.cs
│   ├── Persistence/Configurations/SellerProfileConfiguration.cs
│   ├── Persistence/DatabaseMigrationExtensions.cs
│   ├── Repositories/StoreRepository.cs
│   └── DependencyInjection.cs
├── StoreManagement.API/
│   ├── Endpoints/StoreEndpoints.cs
│   ├── Program.cs
│   └── appsettings.json
```

### New Files — Media
```
src/Microservices/Media/
├── Media.API/
│   ├── Endpoints/MediaEndpoints.cs
│   ├── Services/ImageProcessingService.cs
│   ├── Models/MediaUploadResponse.cs
│   ├── Program.cs
│   └── appsettings.json
```

### Modified Files
- `src/Aspire/Marketplace.AppHost/AppHost.cs` — Register storeApi, mediaApi, Azurite, Scalar

---

## Errors Encountered
| Error | Attempt | Resolution |
|:---|:---|:---|
| (none yet) | | |

---

## Progress Log
| Time | Action | Status |
|:---|:---|:---|
| | Plan created | complete |
