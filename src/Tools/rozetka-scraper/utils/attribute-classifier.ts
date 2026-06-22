import { ProductSpecification } from '../pages/rozetka-product.page';

export interface VariantDetail {
  pid: string;
  skuCode: string;
  name: string;
  price: number;
  images: string[];
  specifications: ProductSpecification[];
}

export function classifyAttributes(variants: VariantDetail[]) {
  const commonAttributes: Record<string, string> = {};
  const variantAttributes: Record<string, Record<string, string>> = {};

  // Gather all unique specification keys across all variants
  const allKeys = new Set<string>();
  for (const v of variants) {
    if (!variantAttributes[v.pid]) {
      variantAttributes[v.pid] = {};
    }
    for (const spec of v.specifications) {
      allKeys.add(spec.key);
    }
  }

  // Determine if a key is common across all variants
  for (const key of allKeys) {
    let isCommon = true;
    let firstValue: string | undefined = undefined;

    for (const v of variants) {
      const spec = v.specifications.find(s => s.key === key);
      
      // If a variant is missing this specification, it's not common
      if (!spec) {
        isCommon = false;
        break;
      }

      if (firstValue === undefined) {
        firstValue = spec.value;
      } else if (firstValue !== spec.value) {
        // If the value differs, it's not common
        isCommon = false;
        break;
      }
    }

    if (isCommon && firstValue !== undefined) {
      commonAttributes[key] = firstValue;
    } else {
      // Attribute differs or is missing on some variants — map it to the specific variants
      for (const v of variants) {
        const spec = v.specifications.find(s => s.key === key);
        if (spec) {
          variantAttributes[v.pid][key] = spec.value;
        }
      }
    }
  }

  return { commonAttributes, variantAttributes };
}
