# Phase 6 — StoreManagement.API & Media.API

**Goal**: Seller management and media storage. Can run in parallel with Phases 3–5.

**Depends on**: Phase 2

**Status**: ✅ Complete (2026-05-15)

## StoreManagement.API Tasks

- [x] **Scaffold Clean Architecture** — Domain (Store aggregate, SellerProfile, VerificationStatus), Application, Infrastructure (`store-db`), API
- [x] **Implement seller verification** — Admin approves/rejects sellers
- [x] **Add YARP route** `/api/stores/**`, register in AppHost
- [x] **Write unit + integration tests** — 17 domain unit tests passing

## Media.API Tasks

- [x] **Create thin service** — Azure Blob Storage via `Aspire.Azure.Storage.Blobs`
- [x] **Implement endpoints** — Upload, retrieve, delete media files
- [x] **Implement image processing** — Resize/thumbnail on upload (SixLabors.ImageSharp)
- [x] **Configure Aspire** — Azurite locally, Azure Storage in cloud
- [x] **Add YARP route** `/api/media/**`, register in AppHost
- [ ] **Write integration tests** — Upload → retrieve → verify (deferred to Phase 8)

## Deliverables
```
src/Microservices/
├── StoreManagement/   (full Clean Architecture — 4 projects)
└── Media/Media.API/   (thin service)
tests/UnitTests/
└── StoreManagement.UnitTests/  (17 tests)
```
