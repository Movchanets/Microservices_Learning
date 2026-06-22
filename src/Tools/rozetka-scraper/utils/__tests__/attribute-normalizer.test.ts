import { describe, it, expect } from 'vitest';
import { normalizeAttributeValue } from '../attribute-normalizer';

describe('normalizeAttributeValue', () => {
  it('trims leading and trailing whitespace', () => {
    expect(normalizeAttributeValue('  Black  ')).toBe('Black');
    expect(normalizeAttributeValue('  Cosmic Orange  ')).toBe('Cosmic Orange');
  });

  it('deduplicates internal spaces', () => {
    expect(normalizeAttributeValue('Cosmic  Orange')).toBe('Cosmic Orange');
    expect(normalizeAttributeValue('Graphite  Black')).toBe('Graphite Black');
  });

  it('standardizes storage units: removes space before unit symbol', () => {
    expect(normalizeAttributeValue('256 ГБ')).toBe('256ГБ');
    expect(normalizeAttributeValue('1 ТБ')).toBe('1ТБ');
    expect(normalizeAttributeValue('512 GB')).toBe('512ГБ');
    expect(normalizeAttributeValue('1 TB')).toBe('1ТБ');
  });

  it('standardizes RAM types: DDR without space', () => {
    expect(normalizeAttributeValue('DDR 5')).toBe('DDR5');
    expect(normalizeAttributeValue('DDR 4')).toBe('DDR4');
    expect(normalizeAttributeValue('LPDDR 5X')).toBe('LPDDR5X');
  });

  it('standardizes screen size: removes space before inch mark', () => {
    expect(normalizeAttributeValue('15.6 "')).toBe('15.6"');
    expect(normalizeAttributeValue('14 "')).toBe('14"');
  });

  it('handles already-normalized values without corruption', () => {
    expect(normalizeAttributeValue('256ГБ')).toBe('256ГБ');
    expect(normalizeAttributeValue('DDR5')).toBe('DDR5');
    expect(normalizeAttributeValue('15.6"')).toBe('15.6"');
  });

  it('returns empty string for empty/null input', () => {
    expect(normalizeAttributeValue('')).toBe('');
    expect(normalizeAttributeValue('   ')).toBe('');
  });
});
