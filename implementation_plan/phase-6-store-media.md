# Phase 6 — StoreManagement.API & Media.API

**Goal**: Seller management and media storage. Can run in parallel with Phases 3–5.

**Depends on**: Phase 2

## StoreManagement.API Tasks

- [ ] **Scaffold Clean Architecture** — Domain (Store aggregate, SellerProfile, VerificationStatus), Application, Infrastructure (`store-db`), API
- [ ] **Implement seller verification** — Admin approves/rejects sellers
- [ ] **Add YARP route** `/api/stores/**`, register in AppHost
- [ ] **Write unit + integration tests**

## Media.API Tasks

- [ ] **Create thin service** — Azure Blob Storage via `Aspire.Azure.Storage.Blobs`
- [ ] **Implement endpoints** — Upload, retrieve, delete media files
- [ ] **Implement image processing** — Resize/thumbnail on upload
- [ ] **Configure Aspire** — Azurite locally, Azure Storage in cloud
- [ ] **Add YARP route** `/api/media/**`, register in AppHost
- [ ] **Write integration tests** — Upload → retrieve → verify

## Deliverables
```
src/Microservices/
├── StoreManagement/   (full Clean Architecture)
└── Media/Media.API/   (thin service)
```
