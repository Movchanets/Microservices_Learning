# Task: Implement Auth Feature Unit Tests (Frontend)

**Goal**: Implement unit tests for the Authentication feature in Angular using Vitest. Target core logic in services, state management (SignalStore), and component behavior.

**Context**: 
- Framework: Angular 21 (Signals, Standalone)
- Testing: Vitest + @angular/build:unit-test
- Location: `src/web/src/app/core/auth/` and `src/web/src/app/features/auth/`
- Reference Plans: `7.1.1-identity.md`, `7.1.2-identity-state.md`

**Action Items**:
1. **AuthService Tests (`auth.service.spec.ts`)**:
   - Test: `login`, `register`, `logout` call the correct BFF endpoints (`/bff/auth/*`).
   - Test: `getUser` retrieves the current session user.
   - Test: `ensureCsrf` correctly handles the GET request to `/bff/csrf`.
2. **AuthStore Tests (`auth.store.spec.ts`)**:
   - Test: Initial state has `user: null` and `isAuthenticated: false`.
   - Test: `login` method updates state on success.
   - Test: `logout` method clears user state.
3. **LoginComponent Tests (`login.spec.ts`)**:
   - Test: Form validation (email required, valid format).
   - Test: Submit button is disabled when form is invalid.
   - Test: Calling `login` on the store when form is submitted.
4. **RegisterComponent Tests (`register.spec.ts`)**:
   - Test: Password matching validation.
   - Test: Loading state display during registration.

**Validation**:
- Run: `cd src/web && pnpm run test --watch=false`
- Ensure all tests in `auth` related folders pass.
