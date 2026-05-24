# Test Plan: Identity Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | UserTests, RegisterUserHandlerTests, RegisterUserValidatorTests, LoginUserHandlerTests, ForgotPasswordHandlerTests, GetUserByIdHandlerTests, JwtTokenGeneratorTests, PasswordHasherServiceTests | ~40 | Covered |
| Integration | UserRepositoryTests, IdentityDatabaseTests | ~10 | Partially Covered |
| Contract | IdentityContractTests | ~5 | Partially Covered |
| E2E | login.spec.ts, profile.spec.ts | ~6 | Partially Covered |

## Test Scenarios — Unit

- [x] User creation with valid data
- [x] User creation with empty email throws
- [x] User creation with invalid email throws
- [x] Password hashing roundtrip
- [x] JWT token generation includes claims
- [x] Register handler with new email persists
- [x] Register handler with duplicate email returns error
- [x] Register validator rejects weak password
- [x] Login with valid credentials returns token
- [x] Login with invalid credentials returns error
- [x] Forgot password generates reset token
- [x] GetUserById returns user DTO

## Test Scenarios — Integration

- [x] UserRepository persists and retrieves user
- [x] UserRepository unique email constraint
- [ ] Login flow end-to-end (register → login → token)
- [ ] Token refresh flow
- [ ] Password reset flow end-to-end

## Test Scenarios — E2E

- [x] Login with valid credentials
- [x] Profile display after login
- [ ] Registration happy path (DELETED — re-add)
- [ ] Registration validation errors (DELETED — re-add)
- [ ] Registration duplicate email error (DELETED — re-add)
- [ ] Login validation (empty fields, bad format)
- [ ] Session expiry redirect
- [ ] Token refresh mid-session
- [ ] Forgot password → reset → login with new password

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| Registration E2E tests removed | P0 | Entire register flow untested in E2E |
| Token refresh integration test | P1 | Critical for session persistence |
| Password reset integration | P2 | Happy path only, edge cases missing |
