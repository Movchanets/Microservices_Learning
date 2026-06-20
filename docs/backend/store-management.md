# StoreManagement Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Project Path** | `src/Microservices/StoreManagement/` |

## Key Domain Entities

| Entity | Type | Key Properties |
|:---|:---|:---|
| `Store` | Aggregate Root | Id (PK), SellerId (string), Name, Description, LogoUrl, VerificationStatus, RejectionReason, CreatedAt, UpdatedAt, VerifiedAt, IsVerified (computed) |

### Domain Methods

| Method | Description |
|:---|:---|
| `Create(sellerId, name, description)` | Factory method — creates store with `Pending` status. Fires `StoreCreatedDomainEvent`. |
| `UpdateDetails(name, description)` | Updates store name and description. Sets `UpdatedAt`. |
| `SetLogo(logoUrl)` | Sets store logo URL. Sets `UpdatedAt`. |
| `Verify()` | Sets status to `Verified`, clears RejectionReason, sets `VerifiedAt`. Throws if already verified. Fires `StoreVerifiedDomainEvent`. |
| `Reject(reason)` | Sets status to `Rejected` with reason. Throws if already verified. Clears `VerifiedAt`. |

### Verification Status Flow

```
Pending ──► Verified    (via Verify())
   │
   └──► Rejected       (via Reject(reason))
```

**Guards:**
- `Verify()` throws `InvalidOperationException` if already `Verified`
- `Reject()` throws `InvalidOperationException` if already `Verified`
- Re-rejection from `Rejected` state is allowed (admin can reject again with new reason)

## API Endpoints (`/api/stores`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/` | `CreateStoreCommand` | Seller |
| `GET` | `/` | `ListStoresQuery` | Public (optional `?status=` filter) |
| `GET` | `/{id}` | `GetStoreByIdQuery` | Public |
| `GET` | `/seller/{sellerId}` | `GetStoreBySellerIdQuery` | Public |
| `PUT` | `/{id}` | `UpdateStoreCommand` | Seller (owner) |
| `POST` | `/{id}/verify` | `VerifySellerCommand` | Admin |
| `PUT` | `/{id}/logo` | `SetStoreLogoCommand` | Authenticated (owner) |

**Note:** The `POST /{id}/verify` endpoint handles both verification and rejection via the `IsApproved` boolean in `VerifySellerCommand(storeId, isApproved, reason)`.

## Integration Events

### Published (via Outbox)

| Event | Trigger | Consumers |
|:---|:---|:---|
| `StoreCreatedIntegrationEvent` | `Store.Create()` — `StoreCreatedDomainEvent` | Identity (links StoreId to seller user record) |
| `StoreVerifiedIntegrationEvent` | `Store.Verify()` — `StoreVerifiedDomainEvent` | Catalog (enables product creation), Notification (seller notification) |

## Authorization Policies

| Policy | Roles |
|:---|:---|
| `Seller` | `Seller`, `Admin` |
| `Admin` | `Admin` |

## Current Status & Known Issues

- ✅ Store verification workflow with admin approval/rejection
- ✅ Logo upload support
- ✅ SellerId-to-StoreId linking via events
- ✅ `IsVerified` computed property for convenience
- ⚠️ No rejection reason notification to seller (event only for verification, not rejection)
- ⚠️ `UpdatedAt` and `VerifiedAt` timestamps tracked but not exposed in DTOs

---
*Last Updated: 2026-06-19*
