// Stats card component for admin dashboard overview.
// Displays a single metric with icon, label, and value.

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-stats-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="bg-card/60 backdrop-blur-sm rounded-2xl border border-border p-5">
      <div class="flex items-center gap-3 mb-3">
        <div class="p-2.5 rounded-xl bg-primary/10">
          <lucide-icon [name]="icon()" class="w-5 h-5 text-primary"></lucide-icon>
        </div>
        <span class="text-sm text-muted">{{ label() }}</span>
      </div>
      <p class="text-2xl font-bold font-lexend text-foreground">{{ value() }}</p>
    </div>
  `
})
export class StatsCardComponent {
  label = input.required<string>();
  value = input.required<number | string>();
  icon = input.required<string>();
}
