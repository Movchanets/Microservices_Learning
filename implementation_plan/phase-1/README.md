# Phase 1 — Sub-Plans Index

Execute these sub-tasks **in order**. Each file contains exact commands, project files, and C# code to copy-paste.

| # | Sub-Plan | Description |
|:--|:---|:---|
| 1.1 | [1.1-identity-domain.md](./1.1-identity-domain.md) | Identity.Domain — User aggregate, Role, value objects |
| 1.2 | [1.2-identity-application.md](./1.2-identity-application.md) | Identity.Application — Register, Login, JWT commands/handlers |
| 1.3 | [1.3-identity-infrastructure.md](./1.3-identity-infrastructure.md) | Identity.Infrastructure — EF Core DbContext, repos, JWT service |
| 1.4 | [1.4-identity-api.md](./1.4-identity-api.md) | Identity.API — Minimal API endpoints, Program.cs wiring |
| 1.5 | [1.5-api-gateway.md](./1.5-api-gateway.md) | ApiGateway — YARP, BFF, cookie-to-bearer, CSRF |
| 1.6 | [1.6-apphost-wiring.md](./1.6-apphost-wiring.md) | Wire Identity + Gateway in AppHost, verify full flow |

## Reference Docs
- Architecture: [`plans/README.md`](../../plans/README.md)
- Clean Architecture: [`plans/03-clean-architecture.md`](../../plans/03-clean-architecture.md)
- API Gateway & BFF: [`plans/06-api-gateway-bff.md`](../../plans/06-api-gateway-bff.md)
- Security: [`plans/08-security.md`](../../plans/08-security.md)
- Phase overview: [`implementation_plan/phase-1-identity-gateway.md`](../phase-1-identity-gateway.md)
