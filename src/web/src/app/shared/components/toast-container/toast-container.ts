import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          [class]="toastClass(toast.type)"
          class="px-4 py-3 rounded-xl shadow-lg border flex items-center justify-between gap-3 animate-slide-up">
          <span class="text-sm">{{ toast.message }}</span>
          <button
            (click)="toastService.dismiss(toast.id)"
            class="text-current opacity-50 hover:opacity-100 transition-opacity">
            &times;
          </button>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);

  toastClass(type: string): string {
    const base = '';
    const variants: Record<string, string> = {
      success: `${base} bg-green-500/10 text-green-500 border-green-500/20`,
      error: `${base} bg-red-500/10 text-red-500 border-red-500/20`,
      info: `${base} bg-blue-500/10 text-blue-500 border-blue-500/20`,
    };
    return variants[type] || variants['info'];
  }
}
