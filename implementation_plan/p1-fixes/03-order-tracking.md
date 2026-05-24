# P1-03 — Order Tracking Timeline

**Goal**: Show visual order status progression (Submitted → Inventory Reserved → Paid → Completed).

**Fixes**: MISSING.md #5.3

**Depends on**: P0-05 (SignalR for real-time updates)

---

## Backend

### Add status history to OrderDto

File: `src/Microservices/Ordering/Ordering.Application/DTOs/OrderDto.cs`

Add status history list:
```csharp
public sealed record OrderDto(
    Guid Id,
    string BuyerId,
    string Status,
    List<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderStatusHistoryDto> StatusHistory);
```

### Add status tracking to Order aggregate

File: `src/Microservices/Ordering/Ordering.Domain/Aggregates/Order.cs`

Add `List<OrderStatusHistory>` collection that records each status change with timestamp.

## Frontend

### Order Timeline Component

File: `src/web/src/app/features/orders/order-timeline/order-timeline.ts`

```typescript
@Component({
  selector: 'app-order-timeline',
  template: `
    <div class="flex items-center gap-2">
      @for (step of steps(); track step.status) {
        <div class="flex items-center gap-2">
          <div [class]="stepClass(step)">
            @if (step.completed) {
              <lucide-icon name="Check" class="w-4 h-4"></lucide-icon>
            } @else {
              <span class="w-4 h-4 rounded-full border-2 border-current"></span>
            }
          </div>
          <span class="text-sm">{{ step.label }}</span>
        </div>
        @if (!$last) {
          <div class="flex-1 h-px bg-border"></div>
        }
      }
    </div>
  `
})
export class OrderTimelineComponent {
  order = input.required<Order>();
  steps = computed(() => this.buildSteps(this.order()));

  private buildSteps(order: Order): TimelineStep[] {
    const statuses = ['Submitted', 'InventoryReserved', 'PaymentCompleted', 'Completed'];
    const currentIndex = statuses.indexOf(order.status);
    return statuses.map((s, i) => ({
      status: s,
      label: s.replace(/([A-Z])/g, ' $1').trim(),
      completed: i <= currentIndex,
      current: i === currentIndex,
    }));
  }
}
```

### Add to order detail page

File: `src/web/src/app/features/orders/order-detail/order-detail.ts`

Add `<app-order-timeline [order]="order()" />` at the top of the order detail.

## Done When
- [ ] Order aggregate tracks status history
- [ ] OrderDto includes status history
- [ ] OrderTimelineComponent renders step progression
- [ ] Timeline shown on order detail page
