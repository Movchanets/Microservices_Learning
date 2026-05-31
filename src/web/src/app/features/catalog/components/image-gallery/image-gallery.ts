import { Component, ChangeDetectionStrategy, input, signal, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { GalleryItem } from '../../catalog.models';

@Component({
  selector: 'app-image-gallery',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  host: {
    class: 'block',
  },
  template: `
    <div class="flex flex-col gap-3">
      <!-- Main Image -->
      <div
        class="bg-card border border-border rounded-3xl p-4 md:p-8 flex items-center justify-center min-h-[400px]"
      >
        @if (currentImage()) {
          <img
            [src]="currentImage()!.url"
            [alt]="currentImage()!.fileName"
            class="w-full max-w-md rounded-2xl object-cover shadow-lg"
          />
        } @else if (fallbackUrl()) {
          <img
            [src]="fallbackUrl()!"
            [alt]="'Product image'"
            class="w-full max-w-md rounded-2xl object-cover shadow-lg"
          />
        } @else {
          <lucide-icon name="Package" class="w-32 h-32 text-muted opacity-30"></lucide-icon>
        }
      </div>

      <!-- Thumbnails -->
      @if (gallery().length > 1) {
        <div class="flex gap-2 overflow-x-auto pb-1">
          @for (item of gallery(); track item.id) {
            <button
              (click)="selectImage(item)"
              [class]="item.id === selectedId()
                ? 'flex-shrink-0 w-16 h-16 rounded-lg overflow-hidden border-2 border-primary cursor-pointer'
                : 'flex-shrink-0 w-16 h-16 rounded-lg overflow-hidden border border-border hover:border-primary/50 transition-colors cursor-pointer'"
              [attr.aria-label]="'View ' + item.fileName"
            >
              @if (item.thumbnailUrl) {
                <img
                  [src]="item.thumbnailUrl"
                  [alt]="item.fileName"
                  class="w-full h-full object-cover"
                  loading="lazy"
                />
              } @else {
                <img
                  [src]="item.url"
                  [alt]="item.fileName"
                  class="w-full h-full object-cover"
                  loading="lazy"
                />
              }
            </button>
          }
        </div>
      }
    </div>
  `,
})
export class ImageGalleryComponent {
  gallery = input.required<GalleryItem[]>();
  fallbackUrl = input<string | null>(null);

  selectedId = signal<string | null>(null);

  currentImage = computed(() => {
    const items = this.gallery();
    if (items.length === 0) return null;

    const selected = this.selectedId();
    if (selected) {
      const found = items.find(i => i.id === selected);
      if (found) return found;
    }

    // Default: primary image or first
    return items.find(i => i.isPrimary) ?? items[0];
  });

  selectImage(item: GalleryItem): void {
    this.selectedId.set(item.id);
  }
}
