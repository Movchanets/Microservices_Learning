/**
 * Rozetka Data Transformer
 *
 * Converts scraped Rozetka data into Seeder.App format.
 * Uses rich data from ProductDetails (specs, brand, subtitle, variant prices).
 *
 * ATTRIBUTE SYSTEM:
 * - All attributes extracted dynamically from <dl> specs — no hardcoded key mappings
 * - Attribute type inferred from VALUE pattern (not key name)
 * - Only exception: color name normalization (Ukrainian → English)
 * - Variant selectors provide "selectable" attributes (storage, color, RAM)
 */

import type { ProductSpecification } from '../pages/rozetka-product.page';

// ============================================================================
// Types
// ============================================================================

/**
 * Attribute type determines how the value is handled:
 * - "selectable": user can choose this (from variant selectors) — e.g., storage, color, RAM
 * - "number": numeric value, possibly with unit — e.g., "120 Гц", "233 г"
 * - "text": free text value — e.g., "Apple A19 Pro", "OLED (Super Retina XDR)"
 * - "boolean": yes/no — e.g., "Так", "Ні"
 * - "list": multiple values — e.g., "Bluetooth 6.0NFCWi-Fi"
 * - "color": normalized color name — e.g., "Cosmic Orange"
 * - "resolution": dimensions — e.g., "2868x1320", "48 + 48 + 48 Мп"
 */
export type AttributeType = 'selectable' | 'number' | 'text' | 'boolean' | 'list' | 'color' | 'resolution';

export interface TypedAttribute {
  key: string;        // Original Rozetka key (e.g., "Вбудована пам'ять")
  value: string;      // Original value (e.g., "256 ГБ")
  type: AttributeType;
  normalized?: string; // Cleaned value (e.g., "256 GB", "Cosmic Orange")
}



export interface CategoryConfig {
  storeName: string;
  categoryName: string;
  tags: string[];
}

// ============================================================================
// Utilities
// ============================================================================

export function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s-]/gu, '')
    .replace(/[\s_]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .substring(0, 80);
}

export function generateSku(rozetkaCode: string): string {
  return `ROZ-${rozetkaCode}`;
}

export function parsePrice(priceStr: string): number {
  const prices = priceStr.match(/[\d\s]+(?=₴)/g);
  if (!prices || prices.length === 0) return 0;
  const lastPrice = prices[prices.length - 1];
  return parseInt(lastPrice.replace(/\s/g, ''), 10) || 0;
}

export function mapFilterNameToKey(fName: string): string {
  const lower = fName.toLowerCase();
  if (lower.includes('виробник') || lower.includes('бренд') || lower.includes('producer') || lower.includes('brand')) return 'brand';
  if (lower.includes('колір') || lower.includes('color')) return 'color';
  if (lower.includes('вбудована пам') || lower.includes('обсяг ssd') || lower.includes('об\'єм ssd') || lower.includes('накопичувач') || lower.includes('storage')) return 'storage';
  if (lower.includes('оперативна пам') || lower.includes('ram') || lower.includes('озп')) return 'ram';
  
  return slugifyTransliterated(fName);
}

export function slugifyTransliterated(text: string): string {
  const ukr = {
    'а': 'a', 'б': 'b', 'в': 'v', 'г': 'h', 'ґ': 'g', 'д': 'd', 'е': 'e', 'є': 'ye',
    'ж': 'zh', 'з': 'z', 'и': 'y', 'і': 'i', 'ї': 'yi', 'й': 'y', 'к': 'k', 'л': 'l',
    'м': 'm', 'н': 'n', 'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't', 'у': 'u',
    'ф': 'f', 'х': 'kh', 'ц': 'ts', 'ч': 'ch', 'ш': 'sh', 'щ': 'shch', 'ь': '',
    'ю': 'yu', 'я': 'ya', '\'': '', '’': ''
  };
  
  let res = '';
  for (const char of text.toLowerCase()) {
    res += ukr[char as keyof typeof ukr] !== undefined ? ukr[char as keyof typeof ukr] : char;
  }
  
  return res
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/[\s_]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .substring(0, 50);
}

// ============================================================================
// Color Normalization (the ONLY hardcoded part)
// ============================================================================

/**
 * Normalize a color value to English.
 * Handles Ukrainian color names, compound colors, and Rozetka-specific names.
 */
export function normalizeColor(value: string): string {
  const lower = value.toLowerCase().trim();

  const colorMap: Record<string, string> = {
    'чорний': 'Black', 'чорна': 'Black', 'чорне': 'Black', 'black': 'Black',
    'білий': 'White', 'біла': 'White', 'біле': 'White', 'white': 'White',
    'сірий': 'Gray', 'сіра': 'Gray', 'сіре': 'Gray', 'gray': 'Gray', 'grey': 'Gray',
    'червоний': 'Red', 'червона': 'Red', 'red': 'Red',
    'синій': 'Blue', 'синя': 'Blue', 'blue': 'Blue',
    'зелений': 'Green', 'зелена': 'Green', 'green': 'Green',
    'рожевий': 'Pink', 'рожева': 'Pink', 'pink': 'Pink',
    'золотий': 'Gold', 'золота': 'Gold', 'gold': 'Gold',
    'срібний': 'Silver', 'срібна': 'Silver', 'silver': 'Silver',
    'titanium': 'Titanium',
    'midnight': 'Midnight', 'starlight': 'Starlight',
    'cosmic orange': 'Cosmic Orange', 'deep blue': 'Deep Blue',
    'desert': 'Desert', 'polar blue': 'Polar Blue', 'mint green': 'Mint Green',
    'off white': 'Off White', 'graphite': 'Graphite',
    'luna grey': 'Luna Grey', 'light silver': 'Light Silver',
    'citrus': 'Citrus', 'space gray': 'Space Gray', 'space grey': 'Space Gray',
    'space black': 'Space Black', 'deep purple': 'Deep Purple',
    'natural titanium': 'Natural Titanium', 'blue titanium': 'Blue Titanium',
    'white titanium': 'White Titanium', 'black titanium': 'Black Titanium',
  };

  // Check compound colors first (longest match)
  for (const [pattern, name] of Object.entries(colorMap).sort((a, b) => b[0].length - a[0].length)) {
    if (lower.includes(pattern)) return name;
  }

  // If no match, return original value capitalized
  return value;
}

// ============================================================================
// Dynamic Attribute Type Inference
// ============================================================================

/**
 * Infer the attribute type from the VALUE pattern.
 * No key-based logic — purely value-driven.
 */
export function inferAttributeType(key: string, value: string): AttributeType {
  const lower = value.toLowerCase().trim();

  // Boolean: "Так"/"Ні", "Так"/"Немає", "Без підтримки"
  if (/^(так|ні|немає|є|ніяк|yes|no|true|false|є в наявності|немає в наявності)$/i.test(lower)) {
    return 'boolean';
  }
  if (lower === 'без підтримки карт пам\'яті' || lower === 'без підтримки') {
    return 'boolean';
  }

  // Color: known color names
  if (isColorValue(lower)) {
    return 'color';
  }

  // Resolution: "2868x1320", "48 + 48 + 48 Мп", "4K/3840x2160"
  if (/\d+\s*x\s*\d+/.test(value) || /\d+\s*\+\s*\d+/.test(value)) {
    return 'resolution';
  }

  // Number with unit: "120 Гц", "233 г", "6.9", "40 Вт", "256 ГБ"
  if (/^\d+([.,]\d+)?\s*(Гц|Hz|г|g|Вт|W|мм|mm|см|cm|м|m|дюйм|"|'|Мп|Mp|MP|ГБ|GB|ТБ|TB|мАч|mAh|В|V|кг|kg)$/i.test(value)) {
    return 'number';
  }
  if (/^\d+([.,]\d+)?$/.test(value)) {
    return 'number';
  }

  // List: concatenated values without spaces (Rozetka quirk)
  // e.g., "Bluetooth 6.0NFCWi-Fi" or "2G (GPRS/EDGE)3G (WCDMA)4G (LTE)5G"
  // or "BDSGPSGalileoQZSSГЛОНАССЦифровий компас"
  if (value.length > 15 && !value.includes(', ')) {
    // Check for concatenated words (camelCase or word boundaries)
    const wordCount = (value.match(/[A-ZА-ЯІЇЄҐ][a-zа-яіїєґ]{2,}/g) || []).length;
    if (wordCount >= 3) return 'list';
    // Check for digit-letter transitions
    if (/[a-zA-Z]{2,}\d/.test(value) && /\d[a-zA-Z]{2,}/.test(value)) return 'list';
  }

  // Default: text
  return 'text';
}

function isColorValue(lower: string): boolean {
  const colorWords = [
    'чорний', 'чорна', 'чорне', 'black',
    'білий', 'біла', 'біле', 'white',
    'сірий', 'сіра', 'gray', 'grey',
    'червоний', 'червона', 'red',
    'синій', 'синя', 'blue',
    'зелений', 'зелена', 'green',
    'рожевий', 'рожева', 'pink',
    'золотий', 'золота', 'gold',
    'срібний', 'срібна', 'silver',
    'titanium', 'midnight', 'starlight',
    'cosmic orange', 'deep blue', 'desert',
    'graphite', 'polar blue', 'mint green',
    'off white', 'luna grey', 'light silver',
    'citrus', 'space gray', 'space grey', 'space black',
    'deep purple', 'natural titanium', 'blue titanium',
    'white titanium', 'black titanium',
  ];
  // Use word-boundary-aware matching to avoid "blue" matching in "Bluetooth"
  return colorWords.some(cw => {
    // For multi-word colors, use includes (they're specific enough)
    if (cw.includes(' ')) return lower.includes(cw);
    // For single-word colors, require word boundary
    const regex = new RegExp(`(?:^|[\\s,])${cw}(?:$|[\\s,])`, 'i');
    return regex.test(lower);
  });
}

// ============================================================================
// Dynamic Attribute Builder
// ============================================================================

/**
 * Build typed attributes from ALL specifications.
 * No key filtering — every spec is included.
 * Type is inferred from the value.
 */
export function buildTypedAttributes(specs: ProductSpecification[]): TypedAttribute[] {
  if (!specs || specs.length === 0) return [];

  const attrs: TypedAttribute[] = [];
  const seen = new Set<string>();

  for (const spec of specs) {
    if (!spec.key || !spec.value) continue;

    // Skip generic "Додатково" (Additional) entries — they're long feature lists
    if (spec.key === 'Додатково' && spec.value.length > 200) continue;

    // Skip review pros/cons
    if (spec.key === 'Переваги:' || spec.key === 'Недоліки:') continue;

    // Skip EAN codes
    if (spec.key === 'EAN') continue;

    // Deduplicate by key
    const dedupKey = spec.key;
    if (seen.has(dedupKey)) continue;
    seen.add(dedupKey);

    const type = inferAttributeType(spec.key, spec.value);

    const attr: TypedAttribute = {
      key: spec.key,
      value: spec.value,
      type,
    };

    // Add normalized value for specific types
    if (type === 'color') {
      attr.normalized = normalizeColor(spec.value);
    } else if (type === 'number') {
      attr.normalized = normalizeNumber(spec.value);
    } else if (type === 'list') {
      attr.normalized = normalizeList(spec.value);
    }

    attrs.push(attr);
  }

  return attrs;
}

/**
 * Normalize a number value: "120 Гц" → "120 Hz", "233 г" → "233 g"
 */
export function normalizeNumber(value: string): string {
  return value
    .replace(/Гц/gi, 'Hz')
    .replace(/г$/i, 'g')
    .replace(/Вт/gi, 'W')
    .replace(/мм/gi, 'mm')
    .replace(/см/gi, 'cm')
    .replace(/дюйм/gi, '"')
    .replace(/Мп/gi, 'MP')
    .replace(/ГБ/gi, 'GB')
    .replace(/ТБ/gi, 'TB')
    .replace(/мАч/gi, 'mAh')
    .replace(/кг/gi, 'kg');
}

/**
 * Normalize a list value: "Bluetooth 6.0NFCWi-Fi" → "Bluetooth 6.0, NFC, Wi-Fi"
 */
export function normalizeList(value: string): string {
  // Split on camelCase boundaries and known separators
  return value
    .replace(/([a-zа-яіїєґ])([A-ZА-ЯІЇЄҐ])/g, '$1, $2')
    .replace(/(\d)([A-ZА-ЯІЇЄҐ])/g, '$1, $2')
    .replace(/([a-zа-яіїєґ])(\d)/g, '$1 $2')
    .replace(/\s*,\s*/g, ', ')
    .replace(/^,\s*/, '')
    .replace(/,\s*$/, '');
}

// ============================================================================
// Variant Attribute Extraction (for variant names, not specs)
// ============================================================================

/**
 * Extract structured attributes from a variant name.
 * Only handles storage/size and color from the variant NAME.
 * The variant TYPE comes from the page's selector section.
 */
export function extractVariantAttributes(variantName: string): Record<string, string> {
  const attrs: Record<string, string> = {};
  const lower = variantName.toLowerCase();

  // Storage: "1 ТБ", "256 ГБ", "512 GB"
  const storageMatch = lower.match(/(\d+)\s*(гб|тб|gb|tb)/);
  if (storageMatch) {
    const size = storageMatch[1];
    const unit = storageMatch[2].toUpperCase();
    const normalizedUnit = (unit === 'ГБ' || unit === 'GB') ? 'GB' : 'TB';
    attrs['storage'] = `${size} ${normalizedUnit}`;
  }

  // Color detection from variant name
  const normalized = normalizeColor(variantName);
  if (normalized !== variantName) {
    attrs['color'] = normalized;
  }

  return attrs;
}

/**
 * Build VariantAxes dictionary from variant attributes.
 * Uses the variant TYPE from the page's selector section as the axis key.
 */
export function buildVariantAxes(
  variants: Array<{ Type: string; Attributes?: Record<string, string> }>
): Record<string, string[]> {
  const axes: Record<string, string[]> = {};

  for (const v of variants) {
    // Use the variant's type as the axis key (from selector section)
    if (v.Type === 'model') continue; // Skip model variants

    // Get the axis value from attributes or variant name
    if (v.Attributes) {
      for (const [key, val] of Object.entries(v.Attributes)) {
        if (!axes[key]) axes[key] = [];
        if (!axes[key].includes(val)) axes[key].push(val);
      }
    }
  }

  return Object.keys(axes).length > 0 ? axes : {};
}

// ============================================================================
// Main Transform
// ============================================================================


