import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ImageDownloader } from '../image-downloader';
import * as fs from 'fs/promises';

vi.mock('fs/promises');

describe('ImageDownloader retry', () => {
  let downloader: ImageDownloader;

  beforeEach(() => {
    vi.clearAllMocks();
    downloader = new ImageDownloader('/test/images');
    // Mock fs operations
    vi.mocked(fs.stat).mockRejectedValue(new Error('ENOENT'));
    vi.mocked(fs.mkdir).mockResolvedValue(undefined);
    vi.mocked(fs.writeFile).mockResolvedValue(undefined);
  });

  it('retries up to 3 times on HTTP failure', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
      .mockRejectedValueOnce(new Error('network error'))
      .mockRejectedValueOnce(new Error('timeout'))
      .mockResolvedValueOnce(new Response(new Uint8Array(2048), { status: 200 }));

    const result = await downloader.download('https://example.com/img.jpg', 'slug', 0, 3);

    expect(result).toBe('Images/slug/image0.jpg');
    expect(fetchSpy).toHaveBeenCalledTimes(3);
  });

  it('returns null after exhausting all retries', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new Error('network error'));

    const result = await downloader.download('https://example.com/img.jpg', 'slug', 0, 3);

    expect(result).toBeNull();
    expect(globalThis.fetch).toHaveBeenCalledTimes(4); // initial + 3 retries
  });

  it('succeeds on first try without retry', async () => {
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response(new Uint8Array(2048), { status: 200 }));

    const result = await downloader.download('https://example.com/img.jpg', 'slug', 0, 3);

    expect(result).toBe('Images/slug/image0.jpg');
    expect(globalThis.fetch).toHaveBeenCalledTimes(1);
  });

  it('skips download if file already exists with content', async () => {
    vi.mocked(fs.stat).mockResolvedValue({ size: 5000 } as any);

    const result = await downloader.download('https://example.com/img.jpg', 'slug', 0, 3);

    expect(result).toBe('Images/slug/image0.jpg');
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
});
