# Plan 02: User Profile Hub

## Goal
Transform the current read-only profile page into a comprehensive "Personal Account" hub with sidebar navigation, order history, settings, and real-time notification badges.

## Context
- **Current state:** ProfileComponent shows user name, email, role, and logout button. Read-only. No edit, no order history, no settings.
- **Target state:** Multi-tab dashboard with sidebar navigation (Orders, Messages, Wishlists, Reviews, Settings). Orders tab is the default and most important.
- **Design ref:** `plans/future_design/user_profile.md`
- **Backend gaps:** No update-profile endpoint, no change-password endpoint (MISSING.md #1.3, #6.6)

## Prerequisites
- Identity.API has GET /api/identity/users/{id} (exists)
- Ordering.API has GET /api/orders/buyer/{buyerId} (exists)
- AuthStore has user info with id, email, firstName, lastName, role

## Backend Changes

### 1. Add Update Profile Endpoint
**File:** `src/Microservices/Identity/Identity.API/Endpoints/UserEndpoints.cs`

```csharp
group.MapPut("/{id:guid}/profile", async (
    Guid id,
    UpdateProfileCommand command,
    ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(command with { UserId = id }, ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
})
.WithName("UpdateProfile")
.RequireAuthorization();
```

**New files:**
- `Identity.Application/Commands/UpdateProfile/UpdateProfileCommand.cs`
- `Identity.Application/Commands/UpdateProfile/UpdateProfileHandler.cs`
- `Identity.Application/Commands/UpdateProfile/UpdateProfileValidator.cs`

### 2. Add Change Password Endpoint
**File:** `src/Microservices/Identity/Identity.API/Endpoints/AuthEndpoints.cs`

```csharp
group.MapPost("/change-password", async (
    ChangePasswordCommand command,
    ClaimsPrincipal user,
    ISender sender,
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await sender.Send(command with { UserId = Guid.Parse(userId!) }, ct);
    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
})
.WithName("ChangePassword")
.RequireAuthorization();
```

**New files:**
- `Identity.Application/Commands/ChangePassword/ChangePasswordCommand.cs`
- `Identity.Application/Commands/ChangePassword/ChangePasswordHandler.cs`
- `Identity.Application/Commands/ChangePassword/ChangePasswordValidator.cs`

## Frontend Changes

### 3. Refactor Profile Component into Hub Layout
**File:** `src/web/src/app/features/auth/profile/profile.ts`

Replace current simple layout with:
- **Left sidebar:** User info block (name, email, role) + navigation links
- **Right content:** Active tab content

Sidebar tabs:
- Orders (default) — reuse OrderListComponent
- Settings — profile edit form + change password
- (Future: Messages, Wishlists, Reviews, Viewed Products)

### 4. Create Profile Sidebar Component
**New file:** `src/web/src/app/features/auth/profile/components/profile-sidebar/profile-sidebar.ts`

- User avatar + name + email + role badge
- Navigation links with active state highlighting
- Notification badges (SignalR integration for unread counts)

### 5. Create Profile Settings Component
**New file:** `src/web/src/app/features/auth/profile/components/profile-settings/profile-settings.ts`

- Edit form: firstName, lastName, email (reactive forms)
- Change password form: currentPassword, newPassword, confirmPassword
- Calls `PUT /api/identity/users/{id}/profile` and `POST /api/identity/auth/change-password`

### 6. Create Profile Store
**New file:** `src/web/src/app/features/auth/profile/profile.store.ts`

```typescript
interface ProfileState {
  updating: boolean;
  changingPassword: boolean;
  error: string | null;
  successMessage: string | null;
}
```

Methods: `updateProfile()`, `changePassword()`, `clearMessages()`

### 7. Update Profile Routes
**File:** `src/web/src/app/features/auth/profile/profile.routes.ts` (new or modify)

```typescript
export const PROFILE_ROUTES: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { path: 'orders', loadComponent: () => import('../../orders/order-list/order-list').then(m => m.OrderListComponent) },
  { path: 'settings', loadComponent: () => import('./components/profile-settings/profile-settings').then(m => m.ProfileSettingsComponent) },
];
```

### 8. Update Auth Models
**File:** `src/web/src/app/core/auth/auth.models.ts`

Add `UpdateProfileRequest` and `ChangePasswordRequest` interfaces.

## Files to Modify/Create

| Action | File |
|--------|------|
| CREATE | `Identity.Application/Commands/UpdateProfile/UpdateProfileCommand.cs` |
| CREATE | `Identity.Application/Commands/UpdateProfile/UpdateProfileHandler.cs` |
| CREATE | `Identity.Application/Commands/UpdateProfile/UpdateProfileValidator.cs` |
| CREATE | `Identity.Application/Commands/ChangePassword/ChangePasswordCommand.cs` |
| CREATE | `Identity.Application/Commands/ChangePassword/ChangePasswordHandler.cs` |
| CREATE | `Identity.Application/Commands/ChangePassword/ChangePasswordValidator.cs` |
| MODIFY | `Identity.API/Endpoints/UserEndpoints.cs` |
| MODIFY | `Identity.API/Endpoints/AuthEndpoints.cs` |
| MODIFY | `src/web/src/app/features/auth/profile/profile.ts` |
| CREATE | `src/web/src/app/features/auth/profile/components/profile-sidebar/profile-sidebar.ts` |
| CREATE | `src/web/src/app/features/auth/profile/components/profile-settings/profile-settings.ts` |
| CREATE | `src/web/src/app/features/auth/profile/profile.store.ts` |
| CREATE | `src/web/src/app/features/auth/profile/profile.routes.ts` |
| MODIFY | `src/web/src/app/core/auth/auth.models.ts` |
| MODIFY | `src/web/src/app/core/auth/auth.service.ts` (add updateProfile, changePassword) |
| MODIFY | `src/web/src/app/app.routes.ts` (update profile route to use children) |

## Verification
1. `dotnet build Marketplace.slnx` — no errors
2. `ng build` — no errors
3. `dotnet test tests/UnitTests/Identity.UnitTests/` — passes
4. Manual: Navigate to /profile → shows sidebar + orders tab
5. Manual: Click Settings → edit profile form works
6. Manual: Change password → success/error feedback
7. Manual: Orders tab shows order history
8. Manual: Mobile responsive layout
