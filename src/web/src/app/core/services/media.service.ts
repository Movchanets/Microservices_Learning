import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { GalleryItem } from '../../features/catalog/catalog.models';

/**
 * Service for uploading and managing media via Media.API (through BFF).
 */
@Injectable({ providedIn: 'root' })
export class MediaService {
  private readonly http = inject(HttpClient);

  /**
   * Upload a file to the media gallery for a target (Product or SKU).
   */
  async upload(
    file: File,
    targetId: string,
    targetType: 'Product' | 'SKU',
    isPrimary: boolean = false,
  ): Promise<GalleryItem> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('targetId', targetId);
    formData.append('targetType', targetType);
    formData.append('isPrimary', String(isPrimary));

    return firstValueFrom(
      this.http.post<GalleryItem>('/api/media/upload', formData),
    );
  }

  /**
   * Get gallery for a target.
   */
  async getGallery(targetId: string, targetType: 'Product' | 'SKU'): Promise<GalleryItem[]> {
    return firstValueFrom(
      this.http.get<GalleryItem[]>(`/api/media/gallery/${targetType}/${targetId}`),
    );
  }

  /**
   * Delete a media item.
   */
  async delete(mediaId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`/api/media/${mediaId}`),
    );
  }

  /**
   * Set primary media for a target.
   */
  async setPrimary(targetId: string, targetType: 'Product' | 'SKU', mediaId: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`/api/media/gallery/${targetType}/${targetId}/primary/${mediaId}`, {}),
    );
  }

  /**
   * Reorder gallery items.
   */
  async reorder(targetId: string, targetType: 'Product' | 'SKU', items: { mediaItemId: string; sortOrder: number }[]): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`/api/media/gallery/${targetType}/${targetId}/reorder`, items),
    );
  }
}
