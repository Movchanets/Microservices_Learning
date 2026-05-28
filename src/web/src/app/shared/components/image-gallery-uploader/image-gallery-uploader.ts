import { Component, ChangeDetectionStrategy, input, output, signal, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { GalleryItem } from '../../../features/catalog/catalog.models';

export interface PendingImage {
  file: File;
  previewUrl: string;
  id: string;
}

@Component({
  selector: 'app-image-gallery-uploader',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  host: { class: 'block' },
  template: `
    <div class="space-y-3">
      <!-- Drop zone -->
      <div
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
        (click)="fileInput.click()"
        [class]="isDragOver()
          ? 'border-2 border-dashed border-primary bg-primary/5 rounded-2xl p-6 text-center cursor-pointer transition-colors'
          : 'border-2 border-dashed border-border hover:border-primary/40 rounded-2xl p-6 text-center cursor-pointer transition-colors'"
      >
        <input
          #fileInput
          type="file"
          accept="image/jpeg,image/png,image/gif,image/webp"
          multiple
          (change)="onFilesSelected($event)"
          class="hidden"
        />
        <lucide-icon name="Upload" class="w-8 h-8 mx-auto mb-2 text-muted"></lucide-icon>
        <p class="text-sm text-muted">
          @if (uploading()) {
            Uploading...
          } @else {
            Drag & drop images here or <span class="text-primary font-medium">browse</span>
          }
        </p>
        <p class="text-xs text-muted/60 mt-1">JPEG, PNG, GIF, WebP up to 10MB</p>
      </div>

      @if (error()) {
        <p class="text-xs text-red-500">{{ error() }}</p>
      }
      @if (validationError()) {
        <p class="text-xs text-amber-500">{{ validationError() }}</p>
      }

      <!-- Image grid -->
      @if (hasImages()) {
        <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-2">
          <!-- Uploaded images -->
          @for (img of uploadedImages(); track img.id) {
            <div
              class="relative aspect-square rounded-xl overflow-hidden border-2 cursor-pointer group transition-all"
              [class]="img.isPrimary
                ? 'border-primary shadow-md shadow-primary/20'
                : 'border-border hover:border-primary/40'"
              (click)="setPrimary.emit(img.id)"
            >
              @if (img.thumbnailUrl) {
                <img [src]="img.thumbnailUrl" [alt]="img.fileName" class="w-full h-full object-cover" loading="lazy" />
              } @else {
                <img [src]="img.url" [alt]="img.fileName" class="w-full h-full object-cover" loading="lazy" />
              }

              <!-- Primary badge -->
              @if (img.isPrimary) {
                <div class="absolute bottom-0 inset-x-0 bg-primary/90 text-white text-[10px] font-medium text-center py-0.5">
                  Main
                </div>
              }

              <!-- Remove button -->
              <button
                type="button"
                (click)="$event.stopPropagation(); removeUploaded.emit(img.id)"
                class="absolute top-1 right-1 p-1 bg-black/60 text-white rounded-lg opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-500"
                aria-label="Remove image"
              >
                <lucide-icon name="X" class="w-3 h-3"></lucide-icon>
              </button>
            </div>
          }

          <!-- Pending (not yet uploaded) images -->
          @for (pending of pendingImages(); track pending.id) {
            <div class="relative aspect-square rounded-xl overflow-hidden border-2 border-dashed border-yellow-400/50 group">
              <img [src]="pending.previewUrl" alt="Pending upload" class="w-full h-full object-cover" />
              <div class="absolute bottom-0 inset-x-0 bg-yellow-500/80 text-white text-[10px] font-medium text-center py-0.5">
                Pending
              </div>
              <button
                type="button"
                (click)="removePending.emit(pending.id)"
                class="absolute top-1 right-1 p-1 bg-black/60 text-white rounded-lg opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-500"
                aria-label="Remove image"
              >
                <lucide-icon name="X" class="w-3 h-3"></lucide-icon>
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class ImageGalleryUploaderComponent {
  uploadedImages = input<GalleryItem[]>([]);
  pendingImages = input<PendingImage[]>([]);
  uploading = input(false);
  error = input<string | null>(null);

  filesSelected = output<File[]>();
  removeUploaded = output<string>();
  removePending = output<string>();
  setPrimary = output<string>();

  isDragOver = signal(false);
  validationError = signal<string | null>(null);

  hasImages = computed(() =>
    this.uploadedImages().length > 0 || this.pendingImages().length > 0
  );

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.emitFiles(files);
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.emitFiles(files);
    input.value = '';
  }

  private emitFiles(files: File[]): void {
    this.validationError.set(null);
    const allowed = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    const maxSize = 10 * 1024 * 1024;

    const valid: File[] = [];
    const rejected: string[] = [];

    for (const f of files) {
      if (!allowed.includes(f.type)) {
        rejected.push(`${f.name}: unsupported format`);
      } else if (f.size > maxSize) {
        rejected.push(`${f.name}: exceeds 10MB`);
      } else {
        valid.push(f);
      }
    }

    if (rejected.length > 0) {
      this.validationError.set(rejected.join(', '));
    }

    if (valid.length > 0) {
      this.filesSelected.emit(valid);
    }
  }
}
