# Test Plan: Admin Feature

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | — | 0 | Not Covered |
| Integration | — | 0 | Not Covered |
| E2E | admin-panel.spec.ts | ~6 | Partially Covered |

## Test Scenarios — E2E

- [x] Admin panel display
- [x] Auth guard (redirect unauth)
- [x] Non-admin redirect
- [ ] Admin store detail (DELETED — was in admin-store-detail.spec.ts, 2 tests)
- [ ] Admin user management (DELETED — was in admin-user-management.spec.ts, 5 tests)
- [ ] Payment refund (DELETED — was in payment-refund.spec.ts, 5 tests)
- [ ] Store approval/rejection
- [ ] User role change
- [ ] User deactivation

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| 3 E2E spec files removed | P0 | admin-store-detail, admin-user-management, payment-refund all deleted |
| No unit tests | P1 | Admin logic lives in stores/services — no dedicated unit tests |
| Store verification flow | P1 | Only approve tested, missing rejection |
