import { Component, ChangeDetectionStrategy, input, output, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { CreateReviewRequest } from '../../catalog.models';

@Component({
  selector: 'app-write-review',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, LucideAngularModule],
  template: `
    @if (isOpen()) {
      <div class="p-6 bg-card/40 backdrop-blur-sm border border-border rounded-2xl">
        <h3 class="text-xl font-bold text-foreground font-lexend mb-4">Write a Review</h3>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <!-- Rating Stars -->
          <div class="flex flex-col gap-2">
            <label class="text-sm text-muted-foreground">Your Rating *</label>
            <div class="flex items-center gap-1">
              @for (star of [1, 2, 3, 4, 5]; track star) {
                <button
                  type="button"
                  (click)="form.patchValue({ rating: star })"
                  (mouseenter)="hoveredStar.set(star)"
                  (mouseleave)="hoveredStar.set(0)"
                  class="p-0.5 transition-transform hover:scale-110"
                >
                  <lucide-icon
                    name="Star"
                    [class]="starClass(star)"
                  ></lucide-icon>
                </button>
              }
            </div>
          </div>

          <!-- Title -->
          <div class="flex flex-col gap-2">
            <label for="review-title" class="text-sm text-muted-foreground">Review Title *</label>
            <input
              id="review-title"
              formControlName="title"
              placeholder="Summarize your experience"
              class="px-4 py-3 bg-muted/10 border border-border rounded-xl text-foreground
                     placeholder:text-muted-foreground focus:outline-none focus:border-primary transition-colors"
            />
            @if (form.get('title')?.touched && form.get('title')?.invalid) {
              <span class="text-sm text-red-400">Title is required</span>
            }
          </div>

          <!-- Text -->
          <div class="flex flex-col gap-2">
            <label for="review-text" class="text-sm text-muted-foreground">Your Review *</label>
            <textarea
              id="review-text"
              formControlName="text"
              rows="4"
              placeholder="Tell others about your experience with this product"
              class="px-4 py-3 bg-muted/10 border border-border rounded-xl text-foreground
                     placeholder:text-muted-foreground focus:outline-none focus:border-primary transition-colors resize-y"
            ></textarea>
            @if (form.get('text')?.touched && form.get('text')?.invalid) {
              <span class="text-sm text-red-400">Review text is required</span>
            }
          </div>

          <!-- Submit -->
          <div class="flex items-center gap-3">
            <button
              type="submit"
              [disabled]="form.invalid || submitting()"
              class="px-6 py-3 bg-primary text-white font-semibold rounded-xl
                     hover:bg-secondary active:scale-[0.98] transition-all
                     disabled:opacity-50 disabled:cursor-not-allowed
                     flex items-center gap-2"
            >
              @if (submitting()) {
                <lucide-icon name="Loader" class="w-5 h-5 animate-spin"></lucide-icon>
                Submitting...
              } @else {
                Submit Review
              }
            </button>
            <button
              type="button"
              (click)="isOpen.set(false)"
              class="px-6 py-3 text-muted-foreground hover:text-foreground transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    } @else {
      <button
        (click)="isOpen.set(true)"
        class="px-6 py-3 bg-primary text-white font-semibold rounded-xl
               hover:bg-secondary active:scale-[0.98] transition-all
               flex items-center gap-2"
      >
        <lucide-icon name="Pencil" class="w-5 h-5"></lucide-icon>
        Write a Review
      </button>
    }
  `,
})
export class WriteReviewComponent {
  submitting = input(false);
  submit = output<CreateReviewRequest>();

  protected isOpen = signal(false);
  protected hoveredStar = signal(0);

  private fb = inject(FormBuilder);

  protected form = this.fb.group({
    rating: [0, [Validators.required, Validators.min(1), Validators.max(5)]],
    title: ['', [Validators.required]],
    text: ['', [Validators.required]],
  });

  protected starClass(star: number): string {
    const selected = this.form.get('rating')?.value ?? 0;
    const hovered = this.hoveredStar();
    const active = hovered > 0 ? hovered : selected;

    return star <= active
      ? 'w-6 h-6 text-yellow-400 fill-yellow-400 cursor-pointer'
      : 'w-6 h-6 text-muted cursor-pointer';
  }

  protected onSubmit(): void {
    if (this.form.invalid) return;

    const { rating, title, text } = this.form.value;
    this.submit.emit({
      rating: rating!,
      title: title!,
      text: text!,
    });

    this.form.reset({ rating: 0, title: '', text: '' });
    this.isOpen.set(false);
  }
}
