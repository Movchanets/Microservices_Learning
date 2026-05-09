# 12 — C4 Architecture Diagrams

## Level 1: System Context

```mermaid
graph TB
    subgraph "Users"
        BUYER["👤 Buyer<br/>Browses, purchases products"]
        SELLER["👤 Seller<br/>Lists products, manages store"]
        ADMIN["👤 Admin<br/>Platform management"]
    end

    subgraph "Enterprise Marketplace"
        MKT["🏪 Marketplace Platform<br/>.NET 10 Microservices + Angular SPA"]
    end

    subgraph "External Systems"
        STRIPE["💳 Payment Gateway<br/>Stripe / PayPal"]
        ENTRA["🔐 Microsoft Entra ID<br/>Identity Provider"]
        EMAIL["📧 Email Service<br/>SendGrid / SES"]
    end

    BUYER --> MKT
    SELLER --> MKT
    ADMIN --> MKT
    MKT --> STRIPE
    MKT --> ENTRA
    MKT --> EMAIL
```

## Level 2: Container Diagram

```mermaid
graph TB
    subgraph "Client"
        SPA["Angular SPA<br/>Zard UI + Tailwind<br/>TypeScript"]
    end

    subgraph "Edge"
        GW["API Gateway<br/>YARP + BFF<br/>ASP.NET Core 10"]
    end

    subgraph "Core Services"
        ID["Identity.API<br/>OIDC, JWT, Roles"]
        CAT["Catalog.API<br/>Products, Categories"]
        ORD["Ordering.API<br/>Saga State Machine"]
        INV["Inventory.API<br/>Stock Reservations"]
        PAY["Payment.API<br/>External Gateways"]
        CART["Cart.API<br/>Redis Sessions"]
    end

    subgraph "Supporting Services"
        SRCH["Search.API<br/>Elasticsearch"]
        STORE["StoreManagement.API<br/>Seller Profiles"]
        MEDIA["Media.API<br/>Blob Storage"]
        NOTIF["Notification.Worker<br/>SignalR Push"]
    end

    subgraph "Data Stores"
        PG1["PostgreSQL<br/>(identity_db)"]
        PG2["PostgreSQL<br/>(catalog_db)"]
        PG3["PostgreSQL<br/>(ordering_db)"]
        PG4["PostgreSQL<br/>(inventory_db)"]
        PG5["PostgreSQL<br/>(payment_db)"]
        PG6["PostgreSQL<br/>(store_db)"]
        RED["Redis<br/>(cart + backplane)"]
        ES["Elasticsearch<br/>(search index)"]
        BLOB["Azure Blob<br/>(media files)"]
    end

    subgraph "Messaging"
        BUS["RabbitMQ / Azure SB<br/>(MassTransit)"]
    end

    SPA <-->|HTTPS + Cookie| GW
    GW -->|Bearer| ID & CAT & ORD & INV & PAY & CART & SRCH & STORE & MEDIA
    GW <-->|WebSocket| NOTIF

    ID --> PG1
    CAT --> PG2
    ORD --> PG3
    INV --> PG4
    PAY --> PG5
    STORE --> PG6
    CART --> RED
    SRCH --> ES
    MEDIA --> BLOB
    NOTIF --> RED

    CAT & ORD & INV & PAY & NOTIF <--> BUS
```

## Level 3: Component Diagram (Ordering.API)

```mermaid
graph TB
    subgraph "Ordering.API Container"
        subgraph "Presentation"
            EP["Order Endpoints<br/>Minimal APIs"]
        end

        subgraph "Application"
            CMD["Command Handlers<br/>CreateOrder, CancelOrder"]
            QRY["Query Handlers<br/>GetOrderById, ListOrders"]
            BHV["Pipeline Behaviors<br/>Validation, Logging, Tx"]
            MED["MediatR<br/>Mediator"]
        end

        subgraph "Domain"
            AGG["Order Aggregate<br/>OrderItem, Address"]
            EVT["Domain Events<br/>OrderItemAdded"]
            VO["Value Objects<br/>Money, Address"]
        end

        subgraph "Infrastructure"
            REPO["OrderRepository<br/>EF Core"]
            CTX["OrderDbContext<br/>PostgreSQL"]
            SAGA["OrderStateMachine<br/>MassTransit Saga"]
            CONS["Consumers<br/>InventoryReserved,<br/>PaymentCompleted"]
        end
    end

    EP --> MED
    MED --> CMD & QRY
    CMD & QRY --> BHV
    CMD --> AGG
    AGG --> EVT
    AGG --> VO
    REPO --> CTX
    REPO -.->|implements| CMD
    SAGA <--> CONS
```

## Data Flow: Order Checkout

```mermaid
flowchart LR
    A[Angular SPA] -->|POST /api/cart/checkout| B[YARP BFF]
    B -->|Cookie→Bearer| C[Cart.API]
    C -->|Read Redis| D[(Redis)]
    C -->|Publish| E{OrderSubmittedEvent}
    E -->|MassTransit| F[Ordering.API<br/>Saga Created]
    F -->|ReserveInventoryCmd| G[Inventory.API]
    G -->|InventoryReservedEvent| F
    F -->|ProcessPaymentCmd| H[Payment.API]
    H -->|PaymentCompletedEvent| F
    F -->|OrderCompletedEvent| I[Notification.Worker]
    I -->|SignalR via Redis| A
```
