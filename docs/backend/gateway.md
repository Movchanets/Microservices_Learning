# API Gateway (YARP + BFF)

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Gateway / BFF (no domain logic) |
| **Reverse Proxy** | YARP with Aspire service discovery |
| **Auth** | Cookie session (browser) → Bearer token (downstream) |
| **Project Path** | `src/Gateways/ApiGateway/` |

## Architecture

```
Angular SPA ──► Gateway (Cookie + CSRF) ──► YARP ──► Microservices (Bearer)
                 │
                 ├── /bff/*     (BFF aggregation endpoints)
                 ├── /api/*     (YARP proxy to services)
                 └── /hubs/*    (YARP proxy to Notification SignalR)
```

## Route Map

| Gateway Route | Downstream Service |
|:---|:---|
| `/api/identity/*` | `identity-api` |
| `/api/catalog/*` | `catalog-api` |
| `/api/orders/*` | `ordering-api` |
| `/api/inventory/*` | `inventory-api` |
| `/api/cart/*` | `cart-api` |
| `/api/search/*` | `search-api` |
| `/api/stores/*` | `store-api` |
| `/api/media/*` | `media-api` |
| `/api/payments/*` | `payment-api` |
| `/hubs/*` | `notification-worker` |

## BFF Endpoints

| Method | Path | Description | Auth |
|:---|:---|:---|:---:|
| `POST` | `/bff/auth/login` | Login via Identity, sets session cookie + CSRF | Public |
| `POST` | `/bff/auth/register` | Register via Identity, sets session cookie + CSRF | Public |
| `POST` | `/bff/auth/forgot-password` | Proxy to Identity forgot-password | Public |
| `POST` | `/bff/auth/logout` | Clears session + CSRF cookies | Authenticated |
| `GET` | `/bff/user` | Returns user profile from cookie claims | Authenticated |
| `GET` | `/bff/csrf` | Issues fresh XSRF-TOKEN cookie | Public |
| `GET` | `/bff/cart` | Cart enriched with product details (CartBffService) | Public |
| `GET` | `/bff/orders/buyer/{buyerId}` | Orders enriched with product details (OrderBffService) | Authenticated |
| `GET` | `/bff/orders/{id}` | Single order enriched with details | Authenticated |

## Auth Flow

1. **Browser** → `POST /bff/auth/login` with credentials
2. **Gateway** → `POST /api/identity/auth/login` to Identity service
3. **Identity** → Returns JWT + refresh token
4. **Gateway** → Stores tokens in encrypted `Marketplace.Session` cookie, issues `XSRF-TOKEN` cookie
5. **Subsequent requests** → Gateway reads JWT from cookie, adds `Authorization: Bearer` header, forwards to downstream

**Dev mode:** Also accepts Bearer tokens directly (for Seeder). Uses `DevPolicyScheme` that auto-selects Cookie vs Bearer.

## Security Features

| Feature | Implementation |
|:---|:---|
| **CSRF Protection** | Custom middleware validates `X-XSRF-TOKEN` header on mutating requests (skipped for Bearer) |
| **Cookie → Bearer** | Middleware extracts JWT from session cookie and sets Authorization header |
| **Rate Limiting** | Fixed window: 100 requests/min, queue limit 10 |
| **CORS** | Angular origins (`localhost:4200`, `localhost:4201`), credentials allowed |
| **Request Logging** | `RequestLoggingMiddleware` for observability |

## BFF Services

| Service | Purpose |
|:---|:---|
| `CartBffService` | Enriches cart items with product names/prices from Catalog API |
| `OrderBffService` | Enriches order items with product details from Catalog API |

## Current Status & Known Issues

- ✅ Full BFF pattern with cookie-based auth for SPA
- ✅ CSRF protection with automatic token management
- ✅ YARP reverse proxy with Aspire service discovery
- ✅ Rate limiting configured
- ⚠️ Angular dev server may exit if Gateway is down (dependency)
- ⚠️ Scalar container failed to start (API docs, non-blocking)
