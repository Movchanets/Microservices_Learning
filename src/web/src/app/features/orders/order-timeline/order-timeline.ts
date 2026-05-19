import { Component, ChangeDetectionStrategy, computed, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Order, OrderStatus } from '../../checkout/checkout.models';

interface TimelineStep {
  status: string;
  label: string;
  completed: boolean;
  current: boolean;
  failed: boolean;
}

@Component({
  selector: 'app-order-timeline',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="bg-card rounded-2xl border border-border p-5">
      <h3 class="text-sm font-semibold text-foreground mb-4">Order Progress</h3>
      <div class="flex items-center gap-1">
        @for (step of steps(); track step.status; let last = $last) {
          <div class="flex items-center gap-1 flex-1">
            <div class="flex flex-col items-center gap-1.5 flex-1">
              <div [class]="stepClass(step)" class="w-8 h-8 rounded-full flex items-center justify-center">
                @if (step.completed && !step.failed) {
                  <lucide-icon name="Check" class="w-4 h-4"></lucide-icon>
                } @else if (step.failed) {
                  <lucide-icon name="X" class="w-4 h-4"></lucide-icon>
                } @else if (step.current) {
                  <div class="w-2.5 h-2.5 rounded-full bg-current animate-pulse"></div>
                } @else {
                  <div class="w-2 h-2 rounded-full bg-current opacity-30"></div>
                }
              </div>
              <span [class]="labelClass(step)" class="text-[10px] text-center leading-tight">{{ step.label }}</span>
            </div>
            @if (!last) {
              <div [class]="lineClass(step)" class="h-0.5 flex-1 rounded-full"></div>
            }
          </div>
        }
      </div>
    </div>
  `
})
export class OrderTimelineComponent {
  order = input.required<Order>();

  readonly steps = computed(() => {
    const status = this.order().status;
    const normalFlow: { status: string; label: string }[] = [
      { status: 'Submitted', label: 'Submitted' },
      { status: 'InventoryReserved', label: 'Reserved' },
      { status: 'PaymentProcessing', label: 'Payment' },
      { status: 'Completed', label: 'Completed' },
    ];

    const currentIndex = normalFlow.findIndex(s => s.status === status);
    const isCancelled = status === 'Cancelled';
    const isFaulted = status === 'Faulted';

    return normalFlow.map((step, i) => ({
      status: step.status,
      label: step.label,
      completed: i <= currentIndex && !isCancelled && !isFaulted,
      current: i === currentIndex && !isCancelled && !isFaulted,
      failed: (isCancelled || isFaulted) && i === currentIndex,
    }));
  });

  stepClass(step: TimelineStep): string {
    if (step.failed) return 'bg-red-500/10 text-red-500';
    if (step.current) return 'bg-primary/10 text-primary';
    if (step.completed) return 'bg-green-500/10 text-green-500';
    return 'bg-muted/10 text-muted';
  }

  labelClass(step: TimelineStep): string {
    if (step.failed) return 'text-red-500 font-medium';
    if (step.current) return 'text-primary font-medium';
    if (step.completed) return 'text-green-500';
    return 'text-muted';
  }

  lineClass(step: TimelineStep): string {
    if (step.completed) return 'bg-green-500/30';
    return 'bg-muted/20';
  }
}
