# Auth Feature

## Overview

| Property | Value |
|:---|:---|
| **Feature Path** | `src/web/src/app/features/auth/` |
| **Core Store** | `AuthStore` (in `core/auth/`) — `providedIn: 'root'` |
| **Profile Store** | `ProfileStore` — `providedIn: 'root'` |
| **Route Prefix** | `/auth/*`, `/profile` |
| **Render Mode** | `RenderMode.Prerender` |

## Component Structure

```
auth/
├── login/
│   ├── login.ts               # Login — login form
│   ├── login.html             # External template
│   ├── login.css
│   └── login.spec.ts          # ✅ Tests
├── register/
│   ├── register.ts            # Register — registration form
│   ├── register.html          # External template
│   ├── register.css
│   └── register.spec.ts       # ✅ Tests
├── forgot-password/
│   ├── forgot-password.ts     # ForgotPassword — password reset request
│   └── forgot-password.html
└── profile/
    ├── profile.ts             # ProfileComponent — shell with sidebar
    ├── profile.store.ts       # ProfileStore (root singleton)
    ├── profile.routes.ts      # Named export: PROFILE_ROUTES
    └── components/
        ├── profile-settings/
        │   └── profile-settings.ts  # ProfileSettingsComponent — edit profile form
        └── profile-sidebar/
            └── profile-sidebar.ts   # ProfileSidebarComponent — nav sidebar
```

## SignalStore State Management

### AuthStore (in `core/auth/auth.store.ts`)

| State Property | Type | Description |
|:---|:---|:---|
| `user` | `User \| null` | Current authenticated user |
| `loading` | `boolean` | Auth operation in progress |
| `error` | `string \| null` | Auth error message |

### ProfileStore (root singleton)

| State Property | Type | Description |
|:---|:---|:---|
| `updating` | `boolean` | Profile update in progress |
| `changingPassword` | `boolean` | Password change in progress |
| `error` | `string \| null` | Error message |
| `successMessage` | `string \| null` | Success feedback |

**Key methods:** `updateProfile(id, request)`, `changePassword(request)`, `clearMessages()`

## Key Routes

| Path | Component | Guard |
|:---|:---|:---|
| `/auth/login` | `Login` | None |
| `/auth/register` | `Register` | None |
| `/auth/forgot-password` | `ForgotPassword` | None |
| `/profile` | `ProfileComponent` | `authGuard` |
| `/profile/orders` | `OrderListComponent` | `authGuard` (via children) |
| `/profile/settings` | `ProfileSettingsComponent` | `authGuard` (via children) |

**Note:** `/profile` redirects to `/profile/orders` by default.

## Test Coverage Status

| Spec File | Tests | Status |
|:---|:---|:---|
| `login/login.spec.ts` | ✅ | Passing |
| `register/register.spec.ts` | ✅ | Passing |
| `profile/profile.store.ts` | ❌ | **No tests** |

**E2E Coverage:** Partially covered — `login.spec.ts` (~2 tests), `profile.spec.ts` (~4 tests). Missing: register flow, token refresh, session expiry, forgot password.

## Known Gaps / Issues

- **ProfileStore has 0 unit tests:** `updateProfile()` and `changePassword()` are untested.
- **No token refresh mechanism visible:** Frontend relies on `withCredentials: true` (cookie-based auth). Token refresh is either server-side or not implemented.
- **Forgot password flow incomplete:** Component exists but no E2E or unit tests. Reset token handling is unclear.
- **No email verification step:** Registration flow doesn't show email verification.
- **Profile route reuse:** `profile.routes.ts` imports `OrderListComponent` from `../../orders/` — cross-feature dependency.
- **No social login:** Only email/password authentication supported.
- **`ProfileStore` uses `AuthService` from core** — the `updateProfile` and `changePassword` methods delegate to `AuthService`, which calls the Identity API.
