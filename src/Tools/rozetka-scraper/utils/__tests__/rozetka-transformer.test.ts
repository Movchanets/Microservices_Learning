import { describe, it, expect } from 'vitest';
import {
  normalizeColor,
  inferAttributeType,
  buildTypedAttributes,
  extractVariantAttributes,
  buildVariantAxes,
  normalizeNumber,
  normalizeList,
} from '../rozetka-transformer';

describe('normalizeColor', () => {
  it('normalizes Ukrainian color names to English', () => {
    expect(normalizeColor('чорний')).toBe('Black');
    expect(normalizeColor('білий')).toBe('White');
    expect(normalizeColor('синій')).toBe('Blue');
  });

  it('handles compound colors', () => {
    expect(normalizeColor('Cosmic Orange')).toBe('Cosmic Orange');
    expect(normalizeColor('Natural Titanium')).toBe('Natural Titanium');
  });
});

describe('inferAttributeType', () => {
  it('detects boolean values', () => {
    expect(inferAttributeType('NFC', 'Так')).toBe('boolean');
    expect(inferAttributeType('NFC', 'Ні')).toBe('boolean');
  });

  it('detects number values with units', () => {
    expect(inferAttributeType('Частота', '120 Гц')).toBe('number');
    expect(inferAttributeType('Вага', '233 г')).toBe('number');
    expect(inferAttributeType('Пам\'ять', '256 ГБ')).toBe('number');
  });

  it('detects color values', () => {
    expect(inferAttributeType('Колір', 'чорний')).toBe('color');
    expect(inferAttributeType('Колір', 'Cosmic Orange')).toBe('color');
  });

  it('detects resolution values', () => {
    expect(inferAttributeType('Роздільна здатність', '2868x1320')).toBe('resolution');
  });

  it('defaults to text', () => {
    expect(inferAttributeType('Процесор', 'Apple A19 Pro')).toBe('text');
  });
});

describe('extractVariantAttributes', () => {
  it('extracts storage from variant name', () => {
    const attrs = extractVariantAttributes('iPhone 17 Pro Max 256 ГБ Cosmic Orange');
    expect(attrs['storage']).toBe('256 GB');
  });

  it('extracts color from variant name', () => {
    const attrs = extractVariantAttributes('iPhone 17 Pro Max 256 ГБ Cosmic Orange');
    expect(attrs['color']).toBe('Cosmic Orange');
  });
});

describe('buildVariantAxes', () => {
  it('builds axes from variant attributes', () => {
    const variants = [
      { Type: 'color', Attributes: { color: 'Black', storage: '256 GB' } },
      { Type: 'color', Attributes: { color: 'White', storage: '256 GB' } },
    ];
    const axes = buildVariantAxes(variants);
    expect(axes['color']).toEqual(['Black', 'White']);
    expect(axes['storage']).toEqual(['256 GB']);
  });

  it('skips model variants', () => {
    const variants = [
      { Type: 'model', Attributes: { color: 'Black' } },
    ];
    const axes = buildVariantAxes(variants);
    expect(axes).toEqual({});
  });
});

describe('normalizeNumber', () => {
  it('converts Ukrainian units to English', () => {
    expect(normalizeNumber('120 Гц')).toBe('120 Hz');
    expect(normalizeNumber('256 ГБ')).toBe('256 GB');
    expect(normalizeNumber('40 Вт')).toBe('40 W');
  });
});

describe('normalizeList', () => {
  it('splits concatenated values', () => {
    expect(normalizeList('Bluetooth 6.0NFCWi-Fi')).toContain('Bluetooth 6.0');
  });
});
