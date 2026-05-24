# Test Plan: Home Page & Shared Components

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | header.spec.ts, mega-menu.spec.ts, toast-container.spec.ts, stock-indicator.spec.ts, not-found.spec.ts | ~25 | Covered |
| E2E | not-found.spec.ts, profile-hub.spec.ts | ~7 | Partially Covered |

## Test Scenarios — E2E

- [x] 404 page display
- [x] Profile hub navigation
- [ ] Home page (DELETED — was in home-page.spec.ts, 6 tests)
- [ ] Header mega menu (DELETED — was in header-mega-menu.spec.ts, 6 tests)
- [ ] Header basic (DELETED — was in header.spec.ts, 3 tests)
- [ ] Cart drawer (DELETED — was in cart-drawer.spec.ts, 5 tests)

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| Home page E2E removed | P1 | 6 tests — hero, carousel, category tiles all gone |
| Header E2E removed | P1 | Navigation, mega menu, auth state display |
| Cart drawer E2E removed | P1 | Mini-cart open/close/empty |
