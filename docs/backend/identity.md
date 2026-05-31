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

| Entity | Type | Key Properties |
|:---|:---|:---|
| `User` | Aggregate Root | Email (VO), PasswordHash (VO), FirstName, LastName, Role (flags enum), StoreId, CurrentRefreshToken, IsActive |
| `SavedSearch` | Entity | UserId, Query, FiltersJson, PriceAlertEnabled |
| `RefreshToken` | Value Object | Token, ExpiresAt |

**User Roles** (bitwise flags): `Buyer`, `Seller`, `Admin` — users can hold multiple roles simultaneously.

## API Endpoints

### Auth (`/api/identity/auth`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `POST` | `/register` | `RegisterUserCommand` | Public |
| `POST` | `/login` | `LoginUserCommand` | Public |
| `POST` | `/refresh` | `RefreshTokenCommand` | Public |
| `POST` | `/forgot-password` | `ForgotPasswordCommand` | Public |
| `POST` | `/change-password` | `ChangePasswordCommand` | Authenticated |

### Users (`/api/identity/users`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/` | `ListUsersQuery` | Admin |
| `GET` | `/{id}` | `GetUserByIdQuery` | Authenticated |
| `PUT` | `/{id}/role` | `UpdateUserRoleCommand` | Admin |
| `DELETE` | `/{id}` | `DeactivateUserCommand` | Admin |
| `PUT` | `/{id}/profile` | `UpdateProfileCommand` | Authenticated |

### Saved Searches (`/api/identity/saved-searches`)

| Method | Path | Handler | Auth |
|:---|:---|:---|:---:|
| `GET` | `/` | `GetSavedSearchesQuery` | Authenticated |
| `POST` | `/` | `CreateSavedSearchCommand` | Authenticated |
| `DELETE` | `/{id}` | `DeleteSavedSearchCommand` | Authenticated |

## Integration Events

### Consumed

| Event | Consumer | Action |
|:---|:---|:---|
| `StoreCreatedIntegrationEvent` | `StoreCreatedConsumer` | Links StoreId to seller User |
| `StoreVerifiedIntegrationEvent` | `StoreVerifiedConsumer` | Grants Seller role to user |

### Published

| Event | Trigger |
|:---|:---|
| `UserRegisteredIntegrationEvent` | User.Create() domain event |
| `PasswordResetRequestedIntegrationEvent` | User.GeneratePasswordResetToken() |

## Current Status & Known Issues

- ✅ Full DDD with domain events, value objects, and aggregate root
- ✅ Role management via bitwise flags (multi-role support)
- ✅ MassTransit EF Outbox for reliable event publishing
- ⚠️ No email delivery integration for password reset (event published but no consumer sends email)
