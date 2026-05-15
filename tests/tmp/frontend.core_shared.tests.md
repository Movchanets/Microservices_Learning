# Task: Implement Core & Shared Unit Tests (Frontend)

**Goal**: Implement unit tests for cross-cutting concerns: Interceptors, Theme management, and Shared UI components.

**Context**: 
- Framework: Angular 21
- Testing: Vitest
- Location: `src/web/src/app/core/` and `src/web/src/app/shared/`

**Action Items**:
1. **ApiInterceptor Tests (`api.interceptor.spec.ts`)**:
   - Test: Automatically adds `withCredentials: true` to all requests.
   - Test: Prepends Base URL if applicable (or handles gateway routing).
2. **ThemeService Tests (`theme.service.spec.ts`)**:
   - Test: `toggleTheme` switches between 'light' and 'dark'.
   - Test: Respects `window.matchMedia` for initial system preference.
   - Test: Updates `document.documentElement` class list.
3. **HeaderComponent Tests (`header.spec.ts`)**:
   - Test: Shows "Login/Register" when user is not authenticated.
   - Test: Shows "Profile/Logout" and user name when authenticated.
   - Test: Lucide icons are rendered correctly.
4. **LanguageService Tests (`language.service.spec.ts`)**:
   - Test: Language switching logic and localized string retrieval.

**Validation**:
- Run: `cd src/web && pnpm run test --watch=false`
