# Payment.Application

## Purpose
Application layer for payment processing. Contains the internal command for persisting payment transactions.

## Commands

### ProcessPaymentInternalCommand
- **Input**: `ProcessPaymentInternalCommand(CorrelationId, OrderId, Amount, BuyerId)`
- **Output**: `Result<bool>`
- **Handler**: Creates `PaymentTransaction` aggregate, calls mock gateway for transaction ID, marks as completed, persists.

> Note: The actual gateway call is in `ProcessPaymentConsumer` (Infrastructure). This handler only persists the transaction record.

## Dependencies
- `Payment.Domain` — Aggregate, repository interface
- `BuildingBlocks.Infrastructure` — `Result<T>`
- `MediatR` 14.1.0
