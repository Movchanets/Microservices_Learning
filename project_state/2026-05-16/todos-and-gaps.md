# TODOs and Gaps — 2026-05-16

**Total actionable items: 35** (4 backend, 20 frontend, 8 infrastructure, 3 docs)

---

## Backend TODOs

### Identity.API
| TODO | Priority | Impact |
|------|----------|--------|
| Change-password endpoint | P1 | Users can't change password |
| Update-profile endpoint | P1 | Users can't edit profile |
| Email sending for forgot-password | P2 | Flow is placeholder |
| Email verification on registration | P2 | Unverified emails |

### Cart.API
| TODO | Priority | Impact |
|------|----------|--------|
| Single-item endpoints (POST/PUT/DELETE /items) | P1 | Inefficient full replacement |
| Redis implementation (currently PostgreSQL) | P2 | Deviates from architecture plan |

### Ordering.API
| TODO | Priority | Impact |
|------|----------|--------|
| Cancel order endpoint | P1 | CancelOrderCommand exists but no endpoint |
| Order status update endpoint | P1 | Sellers can't mark as shipped/completed |

### Payment.API
| TODO | Priority | Impact |
|------|----------|--------|
| Refund endpoint | P1 | No way to process refunds |
| Payment method selection | P2 | Only simulated payments |

### Inventory.API
| TODO | Priority | Impact |
|------|----------|--------|
| Low-stock alerts | P2 | No notification mechanism |
| Seller-specific inventory view | P2 | No filtered view |

### Search.API
| TODO | Priority | Impact |
|------|----------|--------|
| Admin reindex endpoint | P2 | Can't rebuild index if corrupted |

### StoreManagement.API
| TODO | Priority | Impact |
|------|----------|--------|
| Store deletion endpoint | P2 | Can't remove stores |

### Notification.Worker
| TODO | Priority | Impact |
|------|----------|--------|
| Target specific users | P1 | Broadcasts to all |

### API Gateway
| TODO | Priority | Impact |
|------|----------|--------|
| Token refresh | P2 | No automatic token renewal |

---

## Frontend TODOs (20 items)

### Catalog (6 TODOs)
| TODO | Priority | Impact |
|------|----------|--------|
| "Add to Cart" button on product detail | P1 | Can't add products to cart from detail page |
| InventoryService for stock checks | P2 | No availability check before add |
| "Sticky Buy Box" | P2 | UX improvement |
| "Frequently Bought Together" section | P2 | Cross-sell opportunity |
| Product variant selector | P2 | No color/size selection |
| Community Q&A and Reviews | P2 | Social proof missing |

### Cart (3 TODOs)
| TODO | Priority | Impact |
|------|----------|--------|
| Slide-out cart drawer | P2 | UX improvement |
| Single-item API calls | P1 | Depends on backend |
| Remove x-buyer-id header pattern | P2 | Legacy code, JWT claims now used |

### Checkout (3 TODOs)
| TODO | Priority | Impact |
|------|----------|--------|
| Address form | P1 | No shipping address collection |
| Payment method selection | P2 | Only simulated |
| Express checkout (Apple Pay, Google Pay) | P2 | Modern payment UX |
| Free shipping progress bar | P2 | Conversion optimization |

### Orders
| TODO | Priority | Impact |
|------|----------|--------|
| Order cancellation UI | P1 | Depends on backend endpoint |

### Seller Dashboard (3 TODOs)
| TODO | Priority | Impact |
|------|----------|--------|
| Inventory management UI | P1 | Sellers can't manage stock |
| Media upload in product form | P2 | No image upload |
| Sales summary endpoint | P2 | Currently hardcoded zeros |
| SellerOrdersComponent bypasses store | P2 | Architectural inconsistency |

### Profile (5 TODOs)
| TODO | Priority | Impact |
|------|----------|--------|
| Edit profile form | P1 | Read-only profile |
| Change password form | P1 | Depends on backend |
| Order history tab | P2 | Reuse OrderListComponent |
| Notification badges | P2 | UX improvement |
| "Personal Account" hub transformation | P2 | Full sidebar navigation |

### Auth
| TODO | Priority | Impact |
|------|----------|--------|
| Email verification flow | P2 | Unverified registrations |

---

## Architecture Gaps

### Testing (~221 total test methods/cases)
| Gap | Status |
|-----|--------|
| Media.UnitTests | ❌ Empty project (no tests) |
| Media.IntegrationTests | ❌ Empty |
| Notification.IntegrationTests | ❌ Empty |
| Ordering.IntegrationTests | ❌ Empty |
| Payment.IntegrationTests | ❌ Empty |
| StoreManagement.IntegrationTests | ❌ Empty |
| Full E2E checkout flow | ❌ Only page load check |
| E2E payment flow | ❌ Missing |
| E2E order creation flow | ❌ Missing |
| api-helpers.ts / db-helpers.ts | ⚠️ Stubs (empty methods) |

### DevOps
| Gap | Status |
|-----|--------|
| CI/CD pipeline | ❌ No GitHub Actions config |
| Dockerfiles | ❌ Aspire handles local only |
| Terraform / IaC | ❌ No infrastructure code |
| Environment-specific config | ❌ Only appsettings.json |

### Cross-Cutting
| Gap | Status |
|-----|--------|
| Email sending | ❌ No email service |
| Email verification | ❌ Unverified users |
| Cart uses PostgreSQL | ⚠️ Deviates from Redis plan |
| Rate limiting | ✅ Implemented (100 req/min) |
| Request logging | ✅ Implemented |
| CSRF protection | ✅ Implemented |

---

## Event Flow Gaps

### Working Flows
```
Cart → OrderSubmittedEvent → Ordering Saga → Inventory → Payment → Notification → SignalR
Catalog → ProductCreated/Updated/Deleted → Search (ES)
StoreManagement → StoreVerifiedEvent → Identity (role update)
```

### Missing Flows
```
Identity → UserRegisteredEvent → (nothing — no welcome email, no analytics)
Payment → PaymentFailedEvent → Notification → (broadcasts to all, not targeted)
Ordering → OrderCancelledEvent → (saga handles, but no UI trigger)
```

---

## Priority Summary

### P1 (Important for UX) — 8 items
1. Change-password endpoint + UI
2. Update-profile endpoint + UI
3. Single-item cart endpoints
4. Cancel order endpoint + UI
5. Order status update (seller)
6. "Add to Cart" button on product detail
7. Address form in checkout
8. Inventory management UI

### P2 (Nice to have) — 19 items
1. Refund endpoint
2. Email sending (forgot-password)
3. Email verification
4. Low-stock alerts
5. Admin reindex endpoint
6. Store deletion endpoint
7. Targeted notifications (not broadcast)
8. Token refresh in gateway
9. Slide-out cart drawer
10. Media upload in product form
11. InventoryService for stock checks
12. "Sticky Buy Box"
13. "Frequently Bought Together"
14. Product variant selector
15. Community Q&A and Reviews
16. Express checkout (Apple Pay, Google Pay)
17. Free shipping progress bar
18. Sales summary endpoint
19. "Personal Account" hub transformation

### DevOps (Deferred) — 4 items
1. CI/CD pipeline
2. Dockerfiles
3. Terraform / IaC
4. Environment-specific config

### Testing Gaps — 5 items
1. Media.UnitTests (empty)
2. 5 empty integration test projects
3. Full E2E checkout flow
4. E2E payment flow
5. E2E order creation flow
