# Ordering.Application

## Purpose
Application layer implementing CQRS use cases via MediatR. Contains commands, queries, validators, and DTOs. No infrastructure concerns.

## Commands

### CreateOrder
- **Input**: `CreateOrderCommand(BuyerId, List<CreateOrderItemDto>)`
- **Output**: `Result<Guid>` (OrderId)
- **Validation**: BuyerId required, at least one item, each item needs Sku/ProductName/UnitPrice>0/Quantity>0
- **Handler**: Creates `Order` aggregate, adds items, persists via repository

### CancelOrder
- **Input**: `CancelOrderCommand(OrderId, Reason)`
- **Output**: `Result<bool>`
- **Handler**: Loads order, calls `MarkCancelled(reason)`, persists

## Queries

### GetOrderById
- **Input**: `GetOrderByIdQuery(OrderId)`
- **Output**: `Result<OrderDto>`

### ListOrdersByBuyer
- **Input**: `ListOrdersByBuyerQuery(BuyerId)`
- **Output**: `Result<List<OrderDto>>`

## DTOs
- `OrderDto` — Full order representation with items
- `OrderItemDto` — Individual line item with computed `TotalPrice`
- `CreateOrderItemDto` — Input DTO for order creation (Sku, ProductName, UnitPrice, Quantity)

## Dependencies
- `Ordering.Domain` — Aggregates, repository interface
- `BuildingBlocks.Infrastructure` — `Result<T>`, pipeline behaviors
- `MediatR` 14.1.0, `FluentValidation` 12.1.1
