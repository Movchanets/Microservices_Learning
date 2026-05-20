# Changelog — 2026-05-20

## Round 1: Core Deduplication & Angular Cleanup

### .NET
- Created `DatabaseMigrationExtensions.ApplyMigrations<TDbContext>()` in BuildingBlocks
- Created `AuthenticationExtensions.AddMarketplaceAuthentication()` in BuildingBlocks
- Updated 6 services to use shared migration extension
- Updated 9 services to use shared JWT auth
- Created `DomainEventsDbContext` base class
- Migrated CatalogDbContext + StoreDbContext to DomainEventsDbContext

### Angular
- Removed `standalone: true` from 27 components
- Converted @Output/@ViewChild to signal APIs
- Added OnPush to 7 components
- Added isPlatformBrowser guards to 4 files
- Replaced `any` types with proper interfaces (13 files)
- Fixed subscription leak in seller-orders.ts
- Removed unused signals/dead code

## Round 2: Remaining Cleanup & Sync-over-Async

### .NET
- Converted 4 SeedData methods to async (eliminated GetAwaiter().GetResult())
- Created `ElasticsearchInitializer` IHostedService (replaced constructor sync-over-async)
- Migrated InventoryDbContext, OrderingDbContext, IdentityDbContext to DomainEventsDbContext
- Introduced `SearchRequest` record (10 params → 1)

### Angular
- Added isPlatformBrowser guards to auth.store.ts and address-form.ts
- Fixed store-verification.ts malformed HTML
- Replaced `any` types in store.service.ts
- Replaced CommonModule with specific pipe imports
- Converted product-list.ts subscribe to takeUntilDestroyed()
- Fixed index signature access in saved-searches.ts and address-form.ts

## Round 3: Method Splitting & Test Fixes

### .NET
- Split ElasticsearchService.SearchAsync (130 → 28 lines)
- Split ProductEndpoints.MapProductEndpoints (259 lines → 3 methods)
- Extracted AddressRequest from CheckoutCartCommand (7 → 2 params)
- Fixed 268 test compilation errors across 14 files
- Fixed IdentityDbContext integration tests (NoOpPublisher)
- Split ProductEventPublisher into 4 separate files

## Round 4: Feature Implementations

### Verified Purchase Check
- Added HasPurchasedQuery to Ordering service
- Added OrderingApiClient HTTP client to Catalog
- CreateReviewHandler now calls Ordering API to verify purchase
- Added Aspire service reference: Catalog → Ordering

### Password Reset Token
- Added PasswordResetToken/ExpiresAt properties to User aggregate
- Added GeneratePasswordResetToken() domain method
- Created PasswordResetRequestedEvent domain event
- Created PasswordResetRequestedIntegrationEvent
- Created PasswordResetRequestedEventHandler (MediatR → MassTransit)
- ForgotPasswordHandler now generates token and persists it
- Created EF migration for new columns
