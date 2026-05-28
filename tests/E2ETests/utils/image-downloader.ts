/**
 * Rozetka Image Downloader Utility
 * 
 * Handles downloading images from Rozetka CDN to local filesystem.
 * Uses Node.js fetch to avoid CORS issues with browser fetch.
 */

import * as fs from 'fs/promises';
import * as path from 'path';

const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36';

export class ImageDownloader {
  private imagesDir: string;

  constructor(imagesDir: string) {
    this.imagesDir = imagesDir;
  }

  /**
   * Download a single image to local filesystem
   * 
   * @param url - Remote image URL
   * @param slug - Product slug for directory name
   * @param idx - Image index (0-based)
   * @returns Relative path to downloaded image, or null on failure
   */
  async download(url: string, slug: string, idx: number): Promise<string | null> {
    const dir = path.join(this.imagesDir, slug);
    const file = path.join(dir, `image${idx}.jpg`);
    const rel = `Images/${slug}/image${idx}.jpg`;

    // Check if already exists
    try {
      await fs.access(file);
      return rel;
    } catch { /* need to download */ }

    try {
      await fs.mkdir(dir, { recursive: true });
      
      const resp = await fetch(url, {
        headers: {
          'User-Agent': USER_AGENT,
          'Referer': 'https://rozetka.com.ua/',
        },
      });

      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
      
      const buf = Buffer.from(await resp.arrayBuffer());
      await fs.writeFile(file, buf);
      
      return rel;
    } catch (err) {
      console.warn(`  ⚠️ Image download failed: ${err}`);
      return null;
    }
  }

  /**
   * Download multiple images for a product
   * 
   * @param urls - Array of image URLs
   * @param slug - Product slug
   * @param maxImages - Maximum images to download (default: 5)
   * @returns Array of relative paths to downloaded images
   */
  async downloadMultiple(urls: string[], slug: string, maxImages = 5): Promise<string[]> {
    const results: string[] = [];
    
    for (let i = 0; i < Math.min(urls.length, maxImages); i++) {
      const path = await this.download(urls[i], slug, i);
      if (path) results.push(path);
      
      // Small delay between downloads
      await this.randomDelay(200, 600);
    }

    return results;
  }

  /**
   * Check if an image already exists locally
   */
  async exists(slug: string, idx: number): Promise<boolean> {
    const file = path.join(this.imagesDir, slug, `image${idx}.jpg`);
    try {
      await fs.access(file);
      return true;
    } catch {
      return false;
    }
  }

  private randomDelay(min: number, max: number): Promise<void> {
    const delay = Math.floor(Math.random() * (max - min + 1)) + min;
    return new Promise(resolve => setTimeout(resolve, delay));
  }
}
