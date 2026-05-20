# Architectural Review — 2026-05-19

## Topology & Boundaries

The Enterprise Marketplace architecture is composed of **10 specialized microservices** orchestrating business capabilities across independent domains, coordinated using **.NET Aspire** and exposed to the Angular SPA via a secure YARP reverse proxy operating under the BFF (Backend-for-Frontend) security paradigm.

```
                  ┌──────────────────────┐
                  │ Angular Frontend SPA │
                  └──────────┬───────────┘
                             │ (HTTPS)
                             ▼
                  ┌──────────────────────┐
                  │  YARP BFF Gateway    │
                  └──────────┬───────────┘
                             │
            ┌────────────────┼────────────────┐
            ▼ (HTTP / S2S)   ▼                ▼
     ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
     │ Identity     │ │ Catalog      │ │ Ordering     │ ... (10 services total)
     └──────────────┘ └──────────────┘ └──────────────┘
            │                │                │
            ▼ (Outbox)       ▼ (Outbox)       ▼ (Saga Outbox)
     ┌────────────────────────────────────────────────┐
     │           MassTransit / RabbitMQ               │
     └────────────────────────────────────────────────┘
```

---

## 1. Domain Modeling & Service Architecture Enforcements

The codebase successfully adheres to the core rules of **Domain-Driven Design (DDD)** and **Clean Architecture**:

*   **Standard 4-Layer Clean Architecture Services**:
    *   *Services*: `Identity`, `Catalog`, `Inventory`, `Ordering`, `Payment`, `StoreManagement`.
    *   *Domain*: Restricts all external dependencies. Houses aggregates, entities, and domain events. Primary constructor syntax and C# 14 features (`field` keyword) are standard.
    *   *Application*: Implements commands, queries, MediatR handlers, and FluentValidation pipeline validators.
    *   *Infrastructure*: Houses DbContext (acting as UnitOfWork), repositories, and MassTransit consumer bindings.
    *   *API*: Exposes light, focused Minimal APIs mapping request contexts straight to MediatR commands.
*   **Thin Architecture Services**:
    *   *Services*: `Cart`, `Search`, `Media`, `Notification`.
    *   *Approach*: Exclude intermediate application layer structures due to simple, high-throughput CRUD nature. Directly map endpoints to handlers or database queries.

---

## 2. Distributed Messaging & Event-Driven Patterns

MassTransit coordinates asynchronous communications and coordinates transaction rollbacks:

*   **Outbox Pattern**:
    *   MassTransit's transactional outbox is activated across services to guarantee "at-least-once" delivery of events (e.g., `StoreVerifiedEvent`, `OrderSubmittedEvent`). Outbox events are persisted inside Npgsql DbContext scopes, avoiding dual-write fallouts.
*   **Ordering Saga**:
    *   A distributed state machine (`MassTransitStateMachine<OrderState>`) oversees the checkout lifecycle.
    *   *Orchestration Workflow*: `CartCheckout` ➔ `InventoryReservation` ➔ `PaymentProcessing` ➔ `NotificationCompletion`.
    *   *Compensation (Rollbacks)*: On payment failure or inventory depletion, compensating commands are published automatically to cancel the order and issue refunds.

---

## 3. ApiGateway & BFF Security Model

*   **Cookie-to-Bearer Token Bridge**:
    *   The BFF gateway exposes cookie-based authentication endpoints.
    *   Client requests arrive with secure, HttpOnly, SameSite cookies. The YARP BFF decrypts cookies, extracts user claims, and injects JWT authorization headers dynamically before forwarding down-stream. This shields JWT tokens from browser storage vulnerabilities (XSS).
*   **CSRF Protection**:
    *   Anti-forgery cookie verification is performed at the BFF boundary for all state-changing verbs (POST, PUT, DELETE).

---

## 4. Key Architectural Deviations & Remediations

During implementation, certain practical concessions were made that diverge from the initial design docs:

### A. Cart Persistence: PostgreSQL vs. Redis
*   *Design Doc Plan*: Use a high-speed in-memory **Redis** cache for shopping cart items.
*   *Actual Implementation*: Employs an Entity Framework Core PostgreSQL database (`cart-db`).
*   *Tradeoffs*:
    *   *Positives*: Structured JSON queries, relational durability, transactional safety during checkout.
    *   *Negatives*: Slightly higher latency compared to raw Redis caching, database connection pooling constraints.
*   *Remediation Plan*: When traffic levels demand, introduce an abstraction layer (`ICartCache`) to proxy active carts to Redis while retaining Postgres for long-term checkout sessions.

### B. Media.API Local Storage Strategy
*   *Design Doc Plan*: Direct Azure Blob Storage usage.
*   *Actual Dev Setup*: Employs the **Azurite** local emulator via .NET Aspire's container integration (`Aspire.Hosting.Azure.Storage`).
*   *Status*: Healthy and seamless. Ready for direct cloud injection in production without altering the code.
