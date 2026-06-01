import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
} from '@angular/core';
import { VariantAxis, VariantOption } from '../../catalog.models';

/**
 * Known color values mapped to their CSS hex codes.
 * Used to render color swatches with the actual color.
 * Falls back to a text pill for unknown colors.
 */
const COLOR_HEX_MAP: Record<string, string> = {
  black: '#000000',
  white: '#FFFFFF',
  red: '#EF4444',
  blue: '#3B82F6',
  green: '#22C55E',
  yellow: '#EAB308',
  orange: '#F97316',
  purple: '#A855F7',
  pink: '#EC4899',
  grey: '#6B7280',
  gray: '#6B7280',
  silver: '#C0C0C0',
  gold: '#FFD700',
  brown: '#A0522D',
  navy: '#1E3A5F',
  beige: '#F5F5DC',
  teal: '#14B8A6',
  cyan: '#06B6D4',
  indigo: '#6366F1',
  lime: '#84CC16',
  maroon: '#800000',
  olive: '#808000',
  coral: '#FF7F50',
  salmon: '#FA8072',
  turquoise: '#40E0D0',
  lavender: '#E6E6FA',
  tan: '#D2B48C',
  burgundy: '#800020',
  charcoal: '#36454F',
  ivory: '#FFFFF0',
  mint: '#98FB98',
  peach: '#FFE5B4',
  'rose gold': '#B76E79',
  'space gray': '#53565A',
  'midnight': '#191970',
  'starlight': '#F5E6D3',
};

const IS_COLOR_AXIS = new Set(['color', 'colour']);

@Component({
  selector: 'app-variant-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (axes().length > 0) {
      <div class="space-y-5">
        @for (axis of axes(); track axis.key) {
          <div>
            <label class="block text-sm font-medium text-foreground mb-2">
              {{ axis.displayName }}:
              @if (selectedValue(axis.key); as val) {
                <span class="font-semibold text-primary ml-1">{{ val }}</span>
              }
            </label>

            @if (isColorAxis(axis)) {
              <!-- Color Swatches -->
              <div class="flex flex-wrap gap-3">
                @for (value of axis.values; track value) {
                  <button
                    [attr.data-testid]="'variant-' + axis.key + '-' + value"
                    (click)="onSelect(axis.key, value)"
                    [disabled]="!isValueAvailable(axis.key, value)"
                    [class]="getSwatchClasses(axis.key, value)"
                    [attr.aria-label]="value"
                    [attr.aria-pressed]="isSelected(axis.key, value)"
                    [title]="value"
                  >
                    @if (getHexColor(value)) {
                      <span
                        class="block w-full h-full rounded-full border border-border/30"
                        [style.background-color]="getHexColor(value)"
                      ></span>
                    } @else {
                      <span class="text-xs font-medium">{{ getAbbreviation(value) }}</span>
                    }
                  </button>
                }
              </div>
            } @else {
              <!-- Standard Button Pills -->
              <div class="flex flex-wrap gap-2">
                @for (value of axis.values; track value) {
                  <button
                    [attr.data-testid]="'variant-' + axis.key + '-' + value"
                    (click)="onSelect(axis.key, value)"
                    [disabled]="!isValueAvailable(axis.key, value)"
                    [class]="getPillClasses(axis.key, value)"
                    [attr.aria-pressed]="isSelected(axis.key, value)"
                  >
                    {{ value }}
                  </button>
                }
              </div>
            }

            <!-- Unavailable hint -->
            @if (hasUnavailableValues(axis.key)) {
              <p class="text-xs text-muted-foreground mt-1.5">
                Grayed out options are currently unavailable
              </p>
            }
          </div>
        }
      </div>
    }
  `,
})
export class VariantPickerComponent {
  /**
   * Variant axes from the variant matrix API.
   * Each axis has a key, display name, and list of allowed values.
   */
  axes = input.required<VariantAxis[]>();

  /**
   * All variant options (combinations) from the matrix.
   * Used to check which value combinations are available.
   */
  options = input.required<VariantOption[]>();

  /**
   * Currently selected values per axis.
   * Example: { "color": "Black", "storage": "256GB" }
   */
  selected = input<Record<string, string>>({});

  /**
   * Emitted when the user selects a value for an axis.
   */
  variantSelected = output<{ axisKey: string; value: string }>();

  /**
   * Set of axis keys that are color-type (rendered as swatches).
   */
  protected isColorAxis(axis: VariantAxis): boolean {
    return IS_COLOR_AXIS.has(axis.key.toLowerCase());
  }

  protected selectedValue(axisKey: string): string | null {
    return this.selected()[axisKey] ?? null;
  }

  protected isSelected(axisKey: string, value: string): boolean {
    const sel = this.selected()[axisKey];
    return sel?.toLowerCase() === value.toLowerCase();
  }

  /**
   * Checks if a specific value on an axis is available given current selections on OTHER axes.
   * A value is available if at least one option with that value is available.
   */
  protected isValueAvailable(axisKey: string, value: string): boolean {
    const currentSelected = this.selected();
    const otherAxesSelected = Object.entries(currentSelected)
      .filter(([key]) => key !== axisKey);

    // If no other axes have selections, all values are available
    if (otherAxesSelected.length === 0) return true;

    // Check if any option matches this value AND the other selected values
    return this.options().some(option => {
      // Must match this value
      if (option.combination[axisKey]?.toLowerCase() !== value.toLowerCase()) return false;
      // Must match all other selected values
      return otherAxesSelected.every(([otherKey, otherValue]) =>
        option.combination[otherKey]?.toLowerCase() === otherValue.toLowerCase()
      );
    });
  }

  protected hasUnavailableValues(axisKey: string): boolean {
    const axis = this.axes().find(a => a.key === axisKey);
    if (!axis) return false;
    return axis.values.some(v => !this.isValueAvailable(axisKey, v));
  }

  protected getHexColor(value: string): string | null {
    return COLOR_HEX_MAP[value.toLowerCase()] ?? null;
  }

  protected getAbbreviation(value: string): string {
    const words = value.trim().split(/\s+/);
    if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
    return words.map(w => w[0]).join('').toUpperCase().slice(0, 3);
  }

  protected onSelect(axisKey: string, value: string): void {
    if (!this.isValueAvailable(axisKey, value)) return;
    this.variantSelected.emit({ axisKey, value });
  }

  protected getSwatchClasses(axisKey: string, value: string): string {
    const selected = this.isSelected(axisKey, value);
    const available = this.isValueAvailable(axisKey, value);
    const hex = this.getHexColor(value);
    const isLightColor = hex && this.isLightHex(hex);

    const base = 'w-10 h-10 rounded-full flex items-center justify-center cursor-pointer transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2';

    if (!available) {
      return `${base} opacity-30 cursor-not-allowed border-2 border-border`;
    }

    if (selected) {
      return `${base} ring-2 ring-primary ring-offset-2 ring-offset-background scale-110 border-2 border-primary`;
    }

    return `${base} border-2 border-border hover:border-primary/60 hover:scale-105 ${isLightColor ? 'ring-1 ring-inset ring-border/50' : ''}`;
  }

  protected getPillClasses(axisKey: string, value: string): string {
    const selected = this.isSelected(axisKey, value);
    const available = this.isValueAvailable(axisKey, value);

    const base = 'px-4 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-1';

    if (!available) {
      return `${base} bg-muted/5 text-muted-foreground/30 border border-border/30 cursor-not-allowed line-through`;
    }

    if (selected) {
      return `${base} bg-primary text-white border border-primary shadow-sm`;
    }

    return `${base} bg-card border border-border text-foreground hover:border-primary hover:text-primary`;
  }

  private isLightHex(hex: string): boolean {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminance > 0.7;
  }
}
