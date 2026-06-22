/**
 * Variant Axis Classifier
 *
 * Analyzes scraped variant data and auto-classifies which attributes
 * are variant axes (vary across selectors) vs shared specs (constant).
 */

export interface VariantSpec {
  name: string;
  attributes: Record<string, string>;
}

export interface AxisClassification {
  isVariantAxis: boolean;
  values: string[];
}

/**
 * Given a list of variants with their attributes, classify each attribute
 * as a variant axis (varies across variants) or shared (constant).
 *
 * Attributes present on only some variants are treated as variant axes
 * (different product shape = meaningful difference).
 */
export function classifyVariantAxes(variants: VariantSpec[]): Record<string, AxisClassification> {
  if (variants.length === 0) return {};

  // Collect all unique attribute keys
  const allKeys = new Set<string>();
  for (const v of variants) {
    for (const key of Object.keys(v.attributes)) {
      allKeys.add(key);
    }
  }

  const result: Record<string, AxisClassification> = {};

  for (const key of allKeys) {
    const values: string[] = [];
    const seen = new Set<string>();

    for (const v of variants) {
      const val = v.attributes[key];
      if (val === undefined) {
        // Attribute missing on this variant — treat as varying
        continue;
      }
      if (!seen.has(val)) {
        seen.add(val);
        values.push(val);
      }
    }

    // Single value OR all variants have the same value → not a variant axis
    // Multiple distinct values OR missing on some variants → variant axis
    const allHaveAttr = variants.every(v => v.attributes[key] !== undefined);
    const isVariantAxis = values.length > 1 || !allHaveAttr;

    result[key] = { isVariantAxis, values };
  }

  return result;
}
