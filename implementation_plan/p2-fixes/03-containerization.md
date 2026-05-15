# P2-03 — Containerization & IaC

**Goal**: Create Dockerfiles for all services and generate Terraform via Aspirate.

**Fixes**: MISSING.md #9.2, #9.3

---

## Dockerfiles

Each service gets a multi-stage Dockerfile:

File: `src/Microservices/Catalog/Catalog.API/Dockerfile`
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Catalog.API/Catalog.API.csproj", "Catalog.API/"]
RUN dotnet restore "Catalog.API/Catalog.API.csproj"
COPY . .
RUN dotnet publish "Catalog.API/Catalog.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

Repeat for: Identity, Ordering, Payment, Inventory, Cart, Search, StoreManagement, Media, Notification.Worker, ApiGateway.

## Docker Compose (alternative to Aspire for prod-like)

File: `docker-compose.yml`
```yaml
services:
  postgres:
    image: postgres:16
  rabbitmq:
    image: rabbitmq:3-management
  redis:
    image: redis:7
  elasticsearch:
    image: elasticsearch:8.12.0
  catalog-api:
    build: src/Microservices/Catalog/Catalog.API
    depends_on: [postgres, rabbitmq]
  # ... all other services
```

## Aspirate (Aspire → Terraform)

Generate IaC from Aspire AppHost:
```bash
dotnet tool install -g aspirate
aspirate generate --output-path ./infra
```

This generates Kubernetes manifests or Terraform configs from the AppHost definition.

## Done When
- [ ] Dockerfile for each service
- [ ] docker-compose.yml for local prod-like environment
- [ ] Aspirate generates IaC from AppHost
