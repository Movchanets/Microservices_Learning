/**
 * Attribute Value Normalizer
 *
 * Cleans and standardizes Rozetka attribute values:
 * - Trims whitespace
 * - Deduplicates spaces
 * - Standardizes units (ГБ, ТБ, GB, TB, DDR, screen inches)
 * - Normalizes English units to Ukrainian equivalents
 */

/** Map English units to Ukrainian */
const EN_TO_UA_UNITS: [RegExp, string][] = [
  [/(\d)\s*(GB|Gb|gb)/g, '$1ГБ'],
  [/(\d)\s*(TB|Tb|tb)/g, '$1ТБ'],
];

/**
 * Normalize a scraped attribute value.
 * Handles storage units, RAM types, screen sizes, and general whitespace cleanup.
 */
export function normalizeAttributeValue(value: string): string {
  if (!value || !value.trim()) return '';

  let v = value.trim();

  // Deduplicate internal spaces
  v = v.replace(/\s{2,}/g, ' ');

  // Convert English storage units to Ukrainian (512 GB → 512ГБ)
  for (const [pattern, replacement] of EN_TO_UA_UNITS) {
    v = v.replace(pattern, replacement);
  }

  // Standardize Ukrainian storage units: remove space before unit (256 ГБ → 256ГБ)
  v = v.replace(/(\d)\s+(ГБ|Гб|гб)/g, '$1ГБ');
  v = v.replace(/(\d)\s+(ТБ|Тб|тб)/g, '$1ТБ');

  // Standardize RAM types: DDR without space (DDR 5 → DDR5, LPDDR 5X → LPDDR5X)
  v = v.replace(/(DDR)\s+(\d)/g, '$1$2');
  v = v.replace(/(LPDDR)\s+(\d)/g, '$1$2');

  // Standardize screen size: remove space before inch mark (15.6 " → 15.6")
  v = v.replace(/(\d+\.?\d*)\s*"/g, '$1"');
  v = v.replace(/(\d+\.?\d*)\s*″/g, '$1″');

  return v.trim();
}
