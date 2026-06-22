import { describe, it, expect } from 'vitest';
import { classifyVariantAxes, type VariantSpec } from '../variant-axis-classifier';

describe('classifyVariantAxes', () => {
  it('marks attributes that vary across variants as isVariantAxis=true', () => {
    const variants: VariantSpec[] = [
      {
        name: 'Cosmic Orange 256ГБ',
        attributes: { 'Колір': 'Cosmic Orange', 'Пам\'ять': '256ГБ', 'ОЗП': '8ГБ' },
      },
      {
        name: 'Natural Titanium 256ГБ',
        attributes: { 'Колір': 'Natural Titanium', 'Пам\'ять': '256ГБ', 'ОЗП': '8ГБ' },
      },
      {
        name: 'Cosmic Orange 512ГБ',
        attributes: { 'Колір': 'Cosmic Orange', 'Пам\'ять': '512ГБ', 'ОЗП': '8ГБ' },
      },
    ];

    const result = classifyVariantAxes(variants);

    expect(result['Колір']).toEqual({ isVariantAxis: true, values: ['Cosmic Orange', 'Natural Titanium'] });
    expect(result['Пам\'ять']).toEqual({ isVariantAxis: true, values: ['256ГБ', '512ГБ'] });
    expect(result['ОЗП']).toEqual({ isVariantAxis: false, values: ['8ГБ'] });
  });

  it('returns empty map for single variant', () => {
    const variants: VariantSpec[] = [
      { name: 'Single', attributes: { 'Колір': 'Black', 'Пам\'ять': '256ГБ' } },
    ];

    const result = classifyVariantAxes(variants);
    expect(result['Колір']).toEqual({ isVariantAxis: false, values: ['Black'] });
    expect(result['Пам\'ять']).toEqual({ isVariantAxis: false, values: ['256ГБ'] });
  });

  it('handles empty variants array', () => {
    expect(classifyVariantAxes([])).toEqual({});
  });

  it('handles variants with different attribute sets', () => {
    const variants: VariantSpec[] = [
      { name: 'A', attributes: { 'Колір': 'Black', 'Пам\'ять': '256ГБ' } },
      { name: 'B', attributes: { 'Колір': 'White' } }, // missing Пам'ять
    ];

    const result = classifyVariantAxes(variants);
    expect(result['Колір']).toEqual({ isVariantAxis: true, values: ['Black', 'White'] });
    // Missing attribute on one variant — treat as varying (different shape)
    expect(result['Пам\'ять']).toEqual({ isVariantAxis: true, values: ['256ГБ'] });
  });
});
