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
| `Store` | Aggregate Root | SellerId (string), Name, Description, LogoUrl, VerificationStatus, RejectionReason |

### Verification Status Flow

```
Pending → Verified
   │
   └──► Rejected
```

## API Endpoints (`/api/stores`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/` | `CreateStoreCommand` | Seller |
| `GET` | `/` | `ListStoresQuery` | Public (optional status filter) |
| `GET` | `/{id}` | `GetStoreByIdQuery` | Public |
| `GET` | `/seller/{sellerId}` | `GetStoreBySellerIdQuery` | Public |
| `PUT` | `/{id}` | `UpdateStoreCommand` | Seller (owner) |
| `POST` | `/{id}/verify` | `VerifySellerCommand` | Admin |
| `PUT` | `/{id}/logo` | `SetStoreLogoCommand` | Authenticated (owner) |

## Integration Events

### Published (via Outbox)

| Event | Trigger |
|:---|:---|
| `StoreCreatedIntegrationEvent` | Store.Create() — consumed by Identity to link StoreId to seller |
| `StoreVerifiedDomainEvent` | Store.Verify() — consumed by Identity to grant Seller role |

## Current Status & Known Issues

- ✅ Store verification workflow with admin approval
- ✅ Logo upload support
- ✅ SellerId-to-StoreId linking via events
- ⚠️ No rejection reason notification to seller (event only for verification)
