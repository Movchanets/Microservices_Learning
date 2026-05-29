import { APIRequestContext } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const TEST_IMAGES_DIR = path.resolve(__dirname, '../test-data/test-images');

/**
 * Upload an image file to the media service.
 * Uses Playwright's multipart option for file uploads.
 */
export async function uploadMedia(
  api: APIRequestContext,
  filePath: string,
  targetId: string,
  targetType: 'Product' | 'SKU',
  isPrimary = false
): Promise<{ id: string; url: string; thumbnailUrl?: string }> {
  const absolutePath = path.isAbsolute(filePath)
    ? filePath
    : path.join(TEST_IMAGES_DIR, filePath);
  const buffer = fs.readFileSync(absolutePath);
  const fileName = path.basename(absolutePath);

  const response = await api.post('/api/media/upload', {
    multipart: {
      file: {
        name: fileName,
        mimeType: 'image/jpeg',
        buffer,
      },
      targetId,
      targetType,
      isPrimary: String(isPrimary),
    },
  });

  if (!response.ok()) {
    throw new Error(
      `Upload failed: ${response.status()} ${await response.text()}`
    );
  }
  return response.json();
}

/**
 * Retrieve the gallery images for a given target (Product or SKU).
 */
export async function getGallery(
  api: APIRequestContext,
  targetType: string,
  targetId: string
): Promise<
  Array<{
    id: string;
    url: string;
    thumbnailUrl?: string;
    isPrimary: boolean;
  }>
> {
  const response = await api.get(
    `/api/media/gallery/${targetType}/${targetId}`
  );
  if (!response.ok()) return [];
  return response.json();
}

/**
 * Delete a media item by ID.
 */
export async function deleteMedia(
  api: APIRequestContext,
  mediaId: string
): Promise<boolean> {
  const response = await api.delete(`/api/media/${mediaId}`);
  return response.ok();
}

/**
 * Resolve an absolute path to a test image file.
 * If the given filename is already absolute, it is returned as-is.
 */
export function getTestImagePath(fileName: string): string {
  return path.isAbsolute(fileName)
    ? fileName
    : path.join(TEST_IMAGES_DIR, fileName);
}

/**
 * Set the primary image for a gallery target.
 */
export async function setPrimaryMedia(
  api: APIRequestContext,
  targetType: string,
  targetId: string,
  mediaItemId: string
): Promise<boolean> {
  const response = await api.put(
    `/api/media/gallery/${targetType}/${targetId}/primary/${mediaItemId}`
  );
  return response.ok();
}
