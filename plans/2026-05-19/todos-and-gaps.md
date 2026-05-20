# Project Gaps & TODO Backlog — 2026-05-19

## Complete Gaps Inventory

Below is an exhaustive compilation of all unresolved development tasks, architectural deviations, and system improvements, categorized by their critical impact on production readiness.

---

## 🔴 Priority 0: Critical Correctness & Build Blocks

These issues directly block correct execution of verification suites or violate hard rules of the project.

### 1. Relational RAW SQL Usage in InMemory Contract Tests
*   **Affected Service**: `Cart.Infrastructure` / `ContractTests`
*   **Location**: `ProductPriceRepository.UpsertAsync`
*   **Symptom**: `InvalidOperationException` in `CatalogToCartContractTests` due to the use of `.ExecuteSqlRawAsync()` with an InMemory database provider.
*   **Fix Action**: Re-architect `UpsertAsync` to use pure EF Core state management (load, update or create, save) to align with the **"NO RAW SQL"** project instruction, or migrate contract tests to an in-memory SQLite provider.

---

## 🟡 Priority 1: High Priority Integration & Security

These items represent major functional, resilience, or security gaps in the primary flows.

### 2. Payment Outbox Leakage
*   **Affected Service**: `Payment.API`
*   **Issue**: `PaymentRefundedEvent` is published using the in-memory bus directly instead of routing through the MassTransit transactional Outbox.
*   **Risk**: Potential for lost refund notifications if the database commit succeeds but the network fails.

### 3. Missing Endpoint Access Controls
*   **Affected Service**: `Payment.API`
*   **Location**: `GET /api/payments/order/{orderId}`
*   **Issue**: The endpoint lacks ownership verification (does not ensure the calling user is the actual buyer of the order).
*   **Risk**: Unauthorized exposure of payment transactions across buyers.

### 4. Cart Repository Retry Loop Fallthrough
*   **Affected Service**: `Cart.API`
*   **Issue**: On concurrency conflicts, the repository retry loop prints a warning and falls through, returning partial or old state instead of throwing a custom concurrency exception.
*   **Risk**: Silent data loss under high parallel requests.

---

## 🟢 Priority 2: Medium/Low Priority Technical Debt & Polish

### 5. Architectural Deviations
*   **Cart Persistence**: Currently backed by **PostgreSQL** via EF Core instead of **Redis** as detailed in the original architectural blueprints. Correcting this represents a major refactoring effort.

### 6. Identity Service Completeness
*   **Email Dispatch**: Forgotten-password recovery and registration confirmation emails are currently handled by placeholder logging. Production-grade SMTP/SendGrid integration is needed.
*   **Email Verification Gate**: Users are permitted to authenticate and access the system with unverified email addresses.

### 7. Search & Discovery Enhancements
*   **Reindexing Endpoint**: Lack of an administrator endpoint to trigger complete Elasticsearch document reindexing on database corruption or scheme drift.
*   **Deprecations**: Obsolete `NumberRange` search query parameters should be migrated to `Number()` in accordance with newer Elasticsearch client conventions.

### 8. Notification Isolation
*   **SignalR Broadcasts**: Most Notification Hub messages are broadcast globally rather than targeting the specific connected `UserId`/`BuyerId`.

### 9. Gateway Resiliency & Lifecycle
*   **Token Refresh Flow**: No mechanism exists in the YARP reverse proxy / BFF middleware to automatically renew expired JWT bearer tokens using refresh tokens.

### 10. Code Warning Cleanup
*   **API Obsolete Annotations**: 129 warnings are generated during compile time, primarily stemming from ASPDEPR002 (`WithOpenApi` deprecation in ASP.NET Core).
*   **Unread Constructor Parameters**: Warning CS9113 occurs in `RefreshTokenHandler` inside `Identity.Application`.

---

## 🌐 Frontend (Angular) Backlog

### 11. Bundle Optimization
*   **Budget Overrun**: Main bundle size is **589KB**, exceeding the target budget of **500KB**. Auditing lazy loading and routing structure is necessary to reduce initial payload.

### 12. Checkout & Cart Polish
*   **Legacy Header Patterns**: Components must completely remove the deprecated `x-buyer-id` header logic in favor of using authentic JWT token parsing.
*   **Express Checkout**: Implement Apple Pay and Google Pay visual interfaces (simulated).
*   **Free Shipping Indicator**: Visual progress tracker indicating how close a buyer is to free shipping thresholds.

### 13. Seller Management Gaps
*   **Store Deletion**: Missing visual interface to trigger soft store deletions.
*   **Product Media Uploads**: Seller product forms lack native integration with `Media.API` for physical image uploads (currently relies on static asset URLs).
*   **Sales Analytical Dashboard**: Dashboard sales summaries return static placeholders (zeros).

---

## 🧪 Testing Coverage Gaps

### 14. Missing / Empty Test Projects
*   **`Media.API`**: No unit or integration test suite coverage exists.
*   **`Notification.Worker`**: Missing integration tests for MassTransit consumers.
*   **`Payment.API`**: Missing database and consumer integration tests.
*   **`StoreManagement.API`**: Complete lack of infrastructure-level integration tests.

### 15. Frontend Gaps
*   No spec tests exist for the newly introduced **Reviews & Ratings** components, or the advanced **Search Facet** components.
