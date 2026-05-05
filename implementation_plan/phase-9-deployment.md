# Phase 9 — Infrastructure & Deployment

**Goal**: Generate cloud infrastructure from Aspire, deploy to Azure Container Apps, and set up CI/CD.

**Depends on**: Phase 8

## Tasks

- [ ] **Generate Terraform manifests** via Aspirate from `Marketplace.AppHost`
  - `main.tf` — ACA Environment, Container Apps, managed services
  - `variables.tf` — Environment-specific configuration
  - `outputs.tf` — Endpoints, connection strings
- [ ] **Configure Azure managed services** in Terraform
  - Azure Database for PostgreSQL Flexible Server (per-service databases)
  - Azure Cache for Redis
  - Azure Service Bus (replace RabbitMQ)
  - Azure Storage Account (replace Azurite)
  - Log Analytics Workspace
- [ ] **Configure Managed Identities**
  - User-Assigned Managed Identity per microservice
  - RBAC assignments for database, storage, service bus access
  - No secrets in config — `DefaultAzureCredential` everywhere
- [ ] **Set up CI/CD pipeline** (GitHub Actions)
  - Build + unit tests on PR
  - Integration tests (Testcontainers) on PR
  - E2E tests (Playwright) on merge to main
  - `terraform apply` on merge to main (staging → production)
- [ ] **Configure ACA specifics**
  - Ingress rules and custom domains
  - Session affinity for Notification.Worker
  - Auto-scaling rules (min/max replicas)
  - Health probe endpoints
- [ ] **Validate deployment**
  - Smoke test all service health endpoints
  - Verify BFF flow works end-to-end in cloud
  - Verify SignalR WebSocket through YARP + ACA Ingress
  - Verify Saga completes successfully with Azure Service Bus
- [ ] **Set up monitoring**
  - Azure Monitor dashboards
  - Alert rules for error rates, latency thresholds
  - Distributed tracing verification in Application Insights

## Deliverables
```
infra/
├── main.tf
├── variables.tf
├── outputs.tf
└── modules/
    ├── container-apps/
    ├── databases/
    ├── messaging/
    └── storage/

.github/
└── workflows/
    ├── ci.yml        (build + test on PR)
    └── deploy.yml    (terraform apply on main)
```
