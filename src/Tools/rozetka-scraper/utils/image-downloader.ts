/**
 * Image Downloader Utility
 * 
 * Downloads images from Rozetka CDN to local filesystem.
 * Uses Node.js fetch to avoid CORS issues.
 * Includes retry logic for failed downloads.
 */

import * as fs from 'fs/promises';
import * as path from 'path';

const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36';

export class ImageDownloader {
  private imagesDir: string;
  private stats = { downloaded: 0, skipped: 0, failed: 0 };

  constructor(imagesDir: string) {
    this.imagesDir = imagesDir;
  }

  getStats() { return { ...this.stats }; }

  /**
   * Download a single image with retry
   * @returns Relative path (Images/{slug}/image{N}.jpg) or null on failure
   */
  async download(url: string, slug: string, idx: number, retries = 2): Promise<string | null> {
    const dir = path.join(this.imagesDir, slug);
    const file = path.join(dir, `image${idx}.jpg`);
    const rel = `Images/${slug}/image${idx}.jpg`;

    // Skip if exists and has content
    try {
      const stat = await fs.stat(file);
      if (stat.size > 100) {
        this.stats.skipped++;
        return rel;
      }
    } catch { /* download */ }

    for (let attempt = 0; attempt <= retries; attempt++) {
      try {
        await fs.mkdir(dir, { recursive: true });

        const resp = await fetch(url, {
          headers: {
            'User-Agent': USER_AGENT,
            'Referer': 'https://rozetka.com.ua/',
          },
          signal: AbortSignal.timeout(15000),
        });

        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const buf = Buffer.from(await resp.arrayBuffer());
        
        // Validate image (minimum 1KB)
        if (buf.length < 1024) throw new Error(`Too small: ${buf.length} bytes`);
        
        await fs.writeFile(file, buf);
        this.stats.downloaded++;
        return rel;
      } catch (err) {
        if (attempt < retries) {
          await this.randomDelay(1000, 2000);
          continue;
        }
        this.stats.failed++;
        return null;
      }
    }
    return null;
  }

  /**
   * Download multiple images for a product
   */
  async downloadMultiple(urls: string[], slug: string, maxImages = 10): Promise<string[]> {
    const results: string[] = [];

    for (let i = 0; i < Math.min(urls.length, maxImages); i++) {
      const p = await this.download(urls[i], slug, i);
      if (p) results.push(p);
      await this.randomDelay(200, 500);
    }

    return results;
  }

  private randomDelay(min: number, max: number): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
