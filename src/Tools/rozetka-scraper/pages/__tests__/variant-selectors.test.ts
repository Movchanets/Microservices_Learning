import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RozetkaProductPage } from '../../pages/rozetka-product.page';

describe('RozetkaProductPage variant selectors', () => {
  let page: any;
  let pom: RozetkaProductPage;

  beforeEach(() => {
    page = {
      evaluate: vi.fn(),
      goto: vi.fn(),
      url: vi.fn().mockReturnValue('https://rozetka.com.ua/ua/iphone/p543553245/'),
    };
    pom = new RozetkaProductPage(page as any);
  });

  it('detects color swatch selectors', async () => {
    page.evaluate.mockResolvedValueOnce([
      { type: 'color', label: 'Cosmic Orange', value: 'Cosmic Orange', selector: '[data-color="cosmic-orange"]' },
      { type: 'color', label: 'Natural Titanium', value: 'Natural Titanium', selector: '[data-color="natural-titanium"]' },
    ]);

    const selectors = await pom.extractVariantSelectors();
    expect(selectors).toHaveLength(2);
    expect(selectors[0].type).toBe('color');
  });

  it('detects storage tile selectors', async () => {
    page.evaluate.mockResolvedValueOnce([
      { type: 'storage', label: '256ГБ', value: '256ГБ', selector: '[data-storage="256"]' },
      { type: 'storage', label: '512ГБ', value: '512ГБ', selector: '[data-storage="512"]' },
      { type: 'storage', label: '1ТБ', value: '1ТБ', selector: '[data-storage="1024"]' },
    ]);

    const selectors = await pom.extractVariantSelectors();
    expect(selectors).toHaveLength(3);
    expect(selectors.every((s: any) => s.type === 'storage')).toBe(true);
  });

  it('detects RAM selectors', async () => {
    page.evaluate.mockResolvedValueOnce([
      { type: 'ram', label: '8ГБ', value: '8ГБ', selector: '[data-ram="8"]' },
      { type: 'ram', label: '16ГБ', value: '16ГБ', selector: '[data-ram="16"]' },
    ]);

    const selectors = await pom.extractVariantSelectors();
    expect(selectors).toHaveLength(2);
    expect(selectors[0].type).toBe('ram');
  });

  it('detects mixed selector types', async () => {
    page.evaluate.mockResolvedValueOnce([
      { type: 'color', label: 'Black', value: 'Black', selector: '[data-color="black"]' },
      { type: 'storage', label: '256ГБ', value: '256ГБ', selector: '[data-storage="256"]' },
      { type: 'ram', label: '8ГБ', value: '8ГБ', selector: '[data-ram="8"]' },
    ]);

    const selectors = await pom.extractVariantSelectors();
    expect(selectors).toHaveLength(3);
    expect(selectors.map((s: any) => s.type)).toEqual(['color', 'storage', 'ram']);
  });

  it('returns empty array when no selectors found', async () => {
    page.evaluate.mockResolvedValueOnce([]);

    const selectors = await pom.extractVariantSelectors();
    expect(selectors).toEqual([]);
  });
});
