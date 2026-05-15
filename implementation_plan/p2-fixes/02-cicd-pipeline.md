# P2-02 — CI/CD Pipeline

**Goal**: Set up GitHub Actions for build, test, and deploy.

**Fixes**: MISSING.md #9.1

---

## GitHub Actions Workflow

File: `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'pnpm'

      - name: Install dependencies
        run: |
          dotnet restore
          pnpm install

      - name: Build backend
        run: dotnet build --no-restore

      - name: Run backend tests
        run: dotnet test --no-build --verbosity normal

      - name: Build frontend
        run: pnpm nx run web:build

      - name: Run frontend tests
        run: pnpm nx run web:test

      - name: Lint frontend
        run: pnpm nx run web:lint
```

## Deploy Workflow

File: `.github/workflows/deploy.yml`

Trigger on push to main after CI passes. Deploy to Azure Container Apps via Aspirate or Terraform.

## Done When
- [ ] CI workflow runs on push/PR
- [ ] Build + test + lint all pass
- [ ] Deploy workflow configured (placeholder)
