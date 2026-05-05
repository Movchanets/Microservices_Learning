# Phase 1 — Identity.API & API Gateway (YARP BFF)

**Goal**: Implement user authentication/authorization and the secure API gateway with BFF cookie flow.

**Depends on**: Phase 0

## Identity.API Tasks

- [ ] **Scaffold Clean Architecture projects** for Identity service
  - `Identity.Domain/` — User aggregate, Role entity, value objects (Email, Password hash)
  - `Identity.Application/` — RegisterUser, LoginUser, RefreshToken commands/handlers
  - `Identity.Infrastructure/` — EF Core DbContext, `identity-db` PostgreSQL config, migrations
  - `Identity.API/` — Minimal API endpoints
- [ ] **Configure OpenID Connect** — OIDC endpoints for authorization code flow
- [ ] **Implement JWT generation** — Access + Refresh token issuance
- [ ] **Implement role management** — Buyer, Seller, Admin roles with claims
- [ ] **Add FluentValidation** rules for registration/login commands
- [ ] **Register in AppHost** — Wire `Identity.API` with `identity-db` and `messaging` references
- [ ] **Write unit tests** for domain logic (password policy, role assignment)
- [ ] **Write integration tests** with Testcontainers (user registration → DB verification)

## API Gateway (YARP) Tasks

- [ ] **Create `ApiGateway` project** in `src/Gateways/ApiGateway/`
- [ ] **Configure YARP reverse proxy** routes in `appsettings.json`
  - Route `/api/identity/**` → Identity.API
  - Placeholder routes for future services
- [ ] **Implement BFF authentication middleware**
  - OIDC authentication with Identity.API
  - Encrypted session cookie creation (HttpOnly, Secure, SameSite=Strict)
  - Cookie-to-Bearer transform middleware
- [ ] **Implement CSRF protection** — Anti-forgery token validation on mutating requests
- [ ] **Register in AppHost** — Wire gateway with all service references
- [ ] **Verify** — Login flow: Angular → YARP → Identity.API → Cookie set → API call with Bearer

## Deliverables
```
src/
├── Gateways/
│   └── ApiGateway/
└── Microservices/
    └── Identity/
        ├── Identity.Domain/
        ├── Identity.Application/
        ├── Identity.Infrastructure/
        └── Identity.API/
```
