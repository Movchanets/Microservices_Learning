# C4 Architecture Documentation — Marketplace Platform

## Files

| File | Diagrams | Description |
|------|----------|-------------|
| [c4-context.md](c4-context.md) | 2 | Users → Platform, Platform → External Systems |
| [c4-container.md](c4-container.md) | 5 | Gateway routing, data stores, messaging producers, messaging consumers, saga state machine |
| [c4-component.md](c4-component.md) | 4 | Gateway components, ordering saga, catalog fan-out, role promotion |
| [c4-interaction-diagram.md](c4-interaction-diagram.md) | 7 | 3 sequence diagrams, product fan-out, role promotion, gateway routing, data store mapping |

## Diagram Index (18 total)

### Context (c4-context.md)
| # | Diagram | Type |
|---|---------|------|
| 1 | Users → Marketplace | C4Context |
| 2 | Marketplace → External Systems | C4Context |

### Container (c4-container.md)
| # | Diagram | Type |
|---|---------|------|
| 3 | Frontend → Gateway → Services | C4Container |
| 4 | Services → Data Stores | C4Container |
| 5 | Producers → RabbitMQ | C4Container |
| 6 | RabbitMQ → Consumers | C4Container |
| 7 | Saga State Machine | StateDiagram |

### Component (c4-component.md)
| # | Diagram | Type |
|---|---------|------|
| 8 | API Gateway Components | C4Component |
| 9 | Ordering Saga Components | C4Component |
| 10 | Catalog Fan-Out | C4Component |
| 11 | Store → Identity Role Promotion | C4Component |

### Interaction (c4-interaction-diagram.md)
| # | Diagram | Type |
|---|---------|------|
| 12 | Checkout Saga (Success) | Sequence |
| 13 | Checkout Saga (Compensation) | Sequence |
| 14 | Buyer Cancel (Plan 11) | Sequence |
| 15 | Product Event Fan-Out | Graph |
| 16 | Store → Identity Role Promotion | Graph |
| 17 | Gateway Routing | Graph |
| 18 | Data Store Mapping | Graph |

## Design Principle

**Diagrams show structure, tables show detail.** All edge labels, message names, and routing details are in tables below each diagram. This keeps diagrams clean and readable while preserving full detail.
