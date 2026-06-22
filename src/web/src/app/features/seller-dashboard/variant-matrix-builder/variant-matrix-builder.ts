/**
 * Variant Matrix Builder Component.
 *
 * Displays variant axes as a grid where sellers select which values to include.
 * Generates Cartesian product preview and lets sellers exclude specific combinations.
 *
 * Example: For a laptop with RAM=[16GB,32GB] and Storage=[256GB,512GB],
 * shows a 2×2 matrix with checkboxes to include/exclude each combination.
 */

import {
  Component, ChangeDetectionStrategy, input, output, signal, computed,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { AttributeDefinition } from '../../../core/services/category.service';

/** A single combination row in the preview table. */
export interface VariantCombination {
  attributes: Record<string, string>;
  included: boolean;
  skuCode: string;
}

/** Output emitted when the builder confirms selections. */
export interface VariantMatrixOutput {
  variantCombinations: Record<string, string[]>;
  excludedCombinations: string[];
  combinationCount: number;
}

@Component({
  selector: 'app-variant-matrix-builder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  host: { class: 'block' },
  template: `
    <div class="space-y-5">
      <!-- Axis value selectors -->
      @for (axis of axes(); track axis.key) {
        <div class="space-y-2">
          <div class="flex items-center gap-2">
            <label class="text-sm font-medium">{{ axis.displayName }}</label>
            @if (axis.isRequired) {
              <span class="text-red-500 text-xs">required</span>
            }
            <span class="text-xs text-muted">({{ axis.allowedValues.length }} values)</span>
          </div>
          <div class="flex flex-wrap gap-2">
            @for (val of axis.allowedValues; track val) {
              <button
                type="button"
                (click)="toggleAxisValue(axis.key, val)"
                [class]="isValueSelected(axis.key, val)
                  ? 'px-3 py-1.5 rounded-lg text-sm font-medium bg-primary text-white cursor-pointer transition-colors'
                  : 'px-3 py-1.5 rounded-lg text-sm font-medium bg-muted/10 text-muted-foreground hover:bg-muted/20 cursor-pointer transition-colors'"
                [attr.data-testid]="'variant-' + axis.key + '-' + val">
                {{ val }}
              </button>
            }
          </div>
        </div>
      }

      <!-- Combination preview -->
      @if (combinations().length > 0) {
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-medium">
              Combinations ({{ includedCount() }}/{{ combinations().length }})
            </h4>
            <div class="flex gap-2">
              <button type="button" (click)="selectAll()"
                      class="text-xs text-primary hover:underline cursor-pointer">
                Select All
              </button>
              <button type="button" (click)="deselectAll()"
                      class="text-xs text-muted hover:underline cursor-pointer">
                Deselect All
              </button>
            </div>
          </div>

          <!-- Combination table -->
          <div class="border border-border rounded-xl overflow-hidden">
            <div class="max-h-64 overflow-y-auto">
              <table class="w-full text-sm">
                <thead class="bg-muted/5 sticky top-0">
                  <tr>
                    <th class="px-3 py-2 text-left w-10">
                      <input type="checkbox"
                             [checked]="allIncluded()"
                             (change)="toggleAll()"
                             class="rounded accent-primary" />
                    </th>
                    @for (axis of axes(); track axis.key) {
                      <th class="px-3 py-2 text-left text-xs font-medium text-muted uppercase">
                        {{ axis.displayName }}
                      </th>
                    }
                    <th class="px-3 py-2 text-left text-xs font-medium text-muted uppercase">SKU Code</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-border">
                  @for (combo of combinations(); track combo.skuCode; let i = $index) {
                    <tr [class]="combo.included ? '' : 'opacity-40'"
                        [attr.data-testid]="'combo-row-' + i">
                      <td class="px-3 py-2">
                        <input type="checkbox"
                               [checked]="combo.included"
                               (change)="toggleCombination(i)"
                               class="rounded accent-primary" />
                      </td>
                      @for (axis of axes(); track axis.key) {
                        <td class="px-3 py-2">
                          <span class="inline-block px-2 py-0.5 bg-muted/10 rounded text-xs">
                            {{ combo.attributes[axis.key] }}
                          </span>
                        </td>
                      }
                      <td class="px-3 py-2 font-mono text-xs text-muted">{{ combo.skuCode }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      } @else if (axes().length > 0) {
        <div class="text-center py-6 text-sm text-muted">
          Select values above to preview variant combinations
        </div>
      }
    </div>
  `,
})
export class VariantMatrixBuilderComponent {
  /** Attribute definitions where isVariantAxis=true and target='Sku'. */
  axes = input.required<AttributeDefinition[]>();

  /** Optional prefix for auto-generated SKU codes. */
  skuCodePrefix = input<string>('');

  /** Tracks which values are selected per axis. */
  selectedValues = signal<Record<string, Set<string>>>({});

  /** Tracks which combination indices are excluded. */
  excludedIndices = signal<Set<number>>(new Set());

  // ── Axis value toggling ────────────────────────────────

  isValueSelected(axisKey: string, value: string): boolean {
    return this.selectedValues()[axisKey]?.has(value) ?? false;
  }

  toggleAxisValue(axisKey: string, value: string): void {
    this.selectedValues.update(current => {
      const copy = { ...current };
      const set = new Set(copy[axisKey] ?? []);
      if (set.has(value)) {
        set.delete(value);
      } else {
        set.add(value);
      }
      copy[axisKey] = set;
      return copy;
    });
    // Reset exclusions when axis values change (combinations are recalculated)
    this.excludedIndices.set(new Set());
  }

  // ── Combination toggling ───────────────────────────────

  toggleCombination(index: number): void {
    this.excludedIndices.update(current => {
      const copy = new Set(current);
      if (copy.has(index)) {
        copy.delete(index);
      } else {
        copy.add(index);
      }
      return copy;
    });
  }

  toggleAll(): void {
    const combos = this.combinations();
    if (this.allIncluded()) {
      // Deselect all
      this.excludedIndices.set(new Set(combos.map((_, i) => i)));
    } else {
      // Select all
      this.excludedIndices.set(new Set());
    }
  }

  selectAll(): void {
    this.selectedValues.update(() => {
      const copy: Record<string, Set<string>> = {};
      for (const axis of this.axes()) {
        copy[axis.key] = new Set(axis.allowedValues);
      }
      return copy;
    });
    this.excludedIndices.set(new Set());
  }

  deselectAll(): void {
    this.selectedValues.update(() => {
      const copy: Record<string, Set<string>> = {};
      for (const axis of this.axes()) {
        copy[axis.key] = new Set();
      }
      return copy;
    });
    this.excludedIndices.set(new Set());
  }

  /** All generated combinations based on selected values. */
  combinations = computed<VariantCombination[]>(() => {
    const axes = this.axes();
    const selected = this.selectedValues();
    const excluded = this.excludedIndices();
    if (axes.length === 0) return [];

    // Get selected values per axis (only axes with selections)
    const axisSelections: { key: string; values: string[] }[] = [];
    for (const axis of axes) {
      const vals = selected[axis.key];
      if (vals && vals.size > 0) {
        axisSelections.push({ key: axis.key, values: Array.from(vals) });
      }
    }
    if (axisSelections.length === 0) return [];

    // Cartesian product
    const raw = this.cartesianProduct(axisSelections);
    return raw.map((combo, i) => ({
      ...combo,
      included: !excluded.has(i),
    }));
  });

  /** Number of included combinations. */
  includedCount = computed(() =>
    this.combinations().filter(c => c.included).length
  );

  /** Whether all combinations are included. */
  allIncluded = computed(() => {
    const combos = this.combinations();
    return combos.length > 0 && combos.every(c => c.included);
  });

  /** Get the current selection data for the parent to submit. */
  getSelection(): VariantMatrixOutput {
    const selected = this.selectedValues();
    const variantCombinations: Record<string, string[]> = {};
    for (const [key, vals] of Object.entries(selected)) {
      variantCombinations[key] = Array.from(vals);
    }
    const combos = this.combinations();
    const excluded = combos
      .filter(c => !c.included)
      .map(c => Object.entries(c.attributes).map(([k, v]) => `${k}:${v}`).join(','));

    return {
      variantCombinations,
      excludedCombinations: excluded,
      combinationCount: combos.filter(c => c.included).length,
    };
  }

  // ── Cartesian product ──────────────────────────────────

  private cartesianProduct(
    axes: { key: string; values: string[] }[]
  ): VariantCombination[] {
    if (axes.length === 0) return [];

    const prefix = this.skuCodePrefix() || 'SKU';
    const result: VariantCombination[] = [];

    const recurse = (depth: number, current: Record<string, string>): void => {
      if (depth === axes.length) {
        // Generate SKU code from values
        const codeParts = Object.values(current).map(v =>
          v.replace(/[^a-zA-Z0-9]/g, '').toUpperCase().slice(0, 4)
        );
        const skuCode = `${prefix}-${codeParts.join('-')}`;
        result.push({
          attributes: { ...current },
          included: true, // will be overridden by computed
          skuCode,
        });
        return;
      }
      const axis = axes[depth];
      for (const val of axis.values) {
        recurse(depth + 1, { ...current, [axis.key]: val });
      }
    };

    recurse(0, {});
    return result;
  }
}
