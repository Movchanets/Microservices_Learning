# Identity Service

## Overview

| Property | Value |
|:---|:---|
| **Service Type** | Full 4-layer (Domain → Application → Infrastructure → API) |
| **Database** | PostgreSQL (EF Core + Npgsql) |
| **Messaging** | RabbitMQ via MassTransit (with EF Outbox) |
| **Auth** | JWT (issued), Cookie session (consumed via Gateway) |
| **Project Path** | `src/Microservices/Identity/` |

## Key Domain Entities

### Aggregate Root

| Entity | Key Properties |
|:---|:---|
| `User` | Email (VO), PasswordHash (VO), FirstName, LastName, Role (flags enum), StoreId, CurrentRefreshToken, PasswordResetToken, PasswordResetTokenExpiresAt, IsActive, CreatedAt |

### Value Objects

| Value Object | Location | Notes |
|:---|:---|:---|
| `Email` | `Identity.Domain/ValueObjects/Email.cs` | Validated email wrapper |
| `PasswordHash` | `Identity.Domain/ValueObjects/PasswordHash.cs` | Hashed password wrapper |
| `RefreshToken` | `Identity.Domain/ValueObjects/RefreshToken.cs` | Token + ExpiresAt, owned by User |

### Domain Events

| Event | Raised By |
|:---|:---|
| `UserRegisteredEvent` | `User.Create()` |
| `PasswordResetRequestedEvent` | `User.GeneratePasswordResetToken()` |
| `UserRoleChangedEvent` | `User.AddRole()`, `User.RemoveRole()` |

**User Roles** (bitwise flags): `Buyer`, `Seller`, `Admin` — users can hold multiple roles simultaneously.

## API Endpoints

### Auth (`/api/identity/auth`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/register` | `RegisterUserCommand` | Public |
| `POST` | `/login` | `LoginUserCommand` | Public |
| `POST` | `/refresh` | `RefreshTokenCommand` | Public |
| `POST` | `/change-password` | `ChangePasswordCommand` | Authenticated |

### Users (`/api/identity/users`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/` | `ListUsersQuery` | Admin |
| `GET` | `/{id}` | `GetUserByIdQuery` | Authenticated |
| `PUT` | `/{id}/role` | `UpdateUserRoleCommand` | Admin |
| `DELETE` | `/{id}` | `DeactivateUserCommand` | Admin |
| `PUT` | `/{id}/profile` | `UpdateProfileCommand` | Authenticated |

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `StoreCreatedIntegrationEvent` | `StoreCreatedConsumer` | Links StoreId to seller User |
| `StoreVerifiedIntegrationEvent` | `StoreVerifiedConsumer` | Adds Seller role to user + sets StoreId |

### Published

| Event | Trigger | Notes |
|:---|:---|:---|
| `UserRegisteredIntegrationEvent` | `UserRegisteredEvent` domain event handler | Published via `UserRegisteredEventHandler` |
| `PasswordResetRequestedIntegrationEvent` | `User.GeneratePasswordResetToken()` | Contract exists in SharedContracts, no handler yet |

## Current Status & Known Issues

- ✅ Full DDD with domain events, value objects, and aggregate root
- ✅ Role management via bitwise flags (multi-role support)
- ✅ MassTransit EF Outbox for reliable event publishing
- ⚠️ No email delivery integration for password reset (event published but no consumer sends email)
- ⚠️ `ForgotPasswordCommand` endpoint not yet implemented
- ⚠️ `PasswordResetRequestedIntegrationEvent` has no domain-event-to-integration-event handler
- ℹ️ `SavedSearch` entity planned but not yet implemented

---

*Last Updated: 2026-06-19*
