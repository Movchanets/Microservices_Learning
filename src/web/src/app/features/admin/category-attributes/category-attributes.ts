import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  signal,
  computed,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import {
  CategoryService,
  CategoryOption,
  AttributeDefinition,
  CreateAttributeRequest,
} from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-category-attributes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  template: `
    <div class="space-y-6">
      <div>
        <h2 class="text-lg font-semibold font-lexend mb-1">Category Attributes</h2>
        <p class="text-sm text-muted">Define which attributes products in each category can have.
          Attributes marked as "Variant Axis" define SKU combinations (e.g., Color × Storage).</p>
      </div>

      <!-- Category Selector -->
      <div>
        <label class="block text-sm font-medium mb-1.5">Select Category</label>
        <select
          #catSelect
          [value]="selectedCategoryId()"
          (change)="onCategoryChange(catSelect.value)"
          class="w-full max-w-md px-4 py-2.5 bg-background border border-border rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
        >
          <option value="">Choose a category...</option>
          @for (cat of categories(); track cat.id) {
            <option [value]="cat.id">
              {{ cat.parentCategoryId ? '  └ ' : '' }}{{ cat.name }}
            </option>
          }
        </select>
      </div>

      @if (selectedCategoryId()) {
        <!-- Add Attribute Form -->
        <div class="bg-card border border-border rounded-2xl p-6 space-y-4">
          <h3 class="text-sm font-semibold">Add Attribute Definition</h3>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label class="block text-xs text-muted mb-1">Key</label>
              <input
                #keyInput
                [value]="newKey()"
                (input)="newKey.set(keyInput.value)"
                placeholder="e.g. color"
                class="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
              />
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Display Name</label>
              <input
                #nameInput
                [value]="newDisplayName()"
                (input)="newDisplayName.set(nameInput.value)"
                placeholder="e.g. Color"
                class="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
              />
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Target</label>
              <select
                #targetSelect
                [value]="newTarget()"
                (change)="newTarget.set(+targetSelect.value)"
                class="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
              >
                <option [value]="1">SKU</option>
                <option [value]="0">Product</option>
              </select>
            </div>
            <div>
              <label class="block text-xs text-muted mb-1">Value Type</label>
              <select
                #typeSelect
                [value]="newValueType()"
                (change)="newValueType.set(+typeSelect.value)"
                class="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
              >
                <option [value]="2">Select (dropdown)</option>
                <option [value]="0">Text</option>
                <option [value]="1">Number</option>
              </select>
            </div>
          </div>

          @if (newValueType() === 2) {
            <div>
              <label class="block text-xs text-muted mb-1">Allowed Values (comma-separated)</label>
              <input
                #valuesInput
                [value]="newAllowedValues()"
                (input)="newAllowedValues.set(valuesInput.value)"
                placeholder="e.g. Red, Blue, Green"
                class="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none"
              />
            </div>
          }

          <div class="flex items-center gap-4">
            <label class="flex items-center gap-2 text-sm cursor-pointer">
              <input
                #filterableCb
                type="checkbox"
                [checked]="newIsFilterable()"
                (change)="newIsFilterable.set(filterableCb.checked)"
                class="rounded border-border"
              />
              Filterable (search faceting)
            </label>
            <label class="flex items-center gap-2 text-sm cursor-pointer">
              <input
                #requiredCb
                type="checkbox"
                [checked]="newIsRequired()"
                (change)="newIsRequired.set(requiredCb.checked)"
                class="rounded border-border"
              />
              Required
            </label>
            <label class="flex items-center gap-2 text-sm cursor-pointer">
              <input
                #variantCb
                type="checkbox"
                [checked]="newIsVariantAxis()"
                (change)="newIsVariantAxis.set(variantCb.checked)"
                class="rounded border-border"
              />
              Variant Axis
            </label>
          </div>

          <button
            type="button"
            (click)="onAdd()"
            [disabled]="!canAdd() || saving()"
            class="px-4 py-2 bg-primary text-white rounded-xl text-sm font-medium hover:bg-secondary transition-colors disabled:opacity-50 cursor-pointer"
          >
            @if (saving()) {
              Adding...
            } @else {
              <lucide-icon name="Plus" class="w-4 h-4 inline mr-1"></lucide-icon>
              Add Attribute
            }
          </button>
        </div>

        <!-- Attribute List -->
        @if (loading()) {
          <div class="space-y-3 animate-pulse">
            @for (i of [1, 2, 3]; track i) {
              <div class="h-16 bg-muted/20 rounded-xl"></div>
            }
          </div>
        } @else if (attributes().length === 0) {
          <div class="p-8 text-center text-muted bg-card border border-border rounded-2xl">
            No attributes defined for this category.
          </div>
        } @else {
          <div class="space-y-2">
            @for (attr of attributes(); track attr.id) {
              <div
                class="flex items-center justify-between p-4 bg-card border border-border rounded-xl"
                [class.opacity-60]="attr.isInherited"
              >
                <div class="flex items-center gap-4 flex-wrap">
                  <span class="font-mono text-sm font-medium px-2 py-1 bg-muted/10 rounded">
                    {{ attr.key }}
                  </span>
                  <span class="text-sm text-foreground">{{ attr.displayName }}</span>
                  <span class="px-2 py-0.5 text-xs rounded-full"
                    [class]="attr.target === 'Sku'
                      ? 'bg-blue-500/10 text-blue-500 border border-blue-500/20'
                      : 'bg-violet-500/10 text-violet-500 border border-violet-500/20'">
                    {{ attr.target }}
                  </span>
                  <span class="px-2 py-0.5 text-xs rounded-full bg-muted/10 text-muted-foreground border border-border">
                    {{ attr.valueType }}
                  </span>
                  @if (attr.isVariantAxis) {
                    <span class="px-2 py-0.5 text-xs rounded-full bg-green-500/10 text-green-500 border border-green-500/20 font-medium">
                      Variant Axis
                    </span>
                  }
                  @if (attr.isInherited) {
                    <span class="px-2 py-0.5 text-xs rounded-full bg-yellow-500/10 text-yellow-600 border border-yellow-500/20">
                      Inherited
                    </span>
                  }
                  @if (attr.allowedValues.length > 0) {
                    <span class="text-xs text-muted">
                      [{{ attr.allowedValues.join(', ') }}]
                    </span>
                  }
                </div>

                @if (!attr.isInherited) {
                  <button
                    type="button"
                    (click)="onRemove(attr)"
                    class="p-2 text-red-500 hover:bg-red-500/10 rounded-lg transition-colors cursor-pointer"
                    [attr.data-testid]="'remove-attr-' + attr.key"
                    aria-label="Remove attribute"
                  >
                    <lucide-icon name="Trash2" class="w-4 h-4"></lucide-icon>
                  </button>
                }
              </div>
            }
          </div>
        }
      }
    </div>
  `,
})
export class CategoryAttributesComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  categories = signal<CategoryOption[]>([]);
  attributes = signal<AttributeDefinition[]>([]);
  selectedCategoryId = signal('');
  loading = signal(false);
  saving = signal(false);

  // New attribute form state
  newKey = signal('');
  newDisplayName = signal('');
  newTarget = signal(1);       // SKU
  newValueType = signal(2);    // Select
  newAllowedValues = signal('');
  newIsFilterable = signal(true);
  newIsRequired = signal(true);
  newIsVariantAxis = signal(true);

  canAdd = computed(() => {
    const key = this.newKey().trim();
    const displayName = this.newDisplayName().trim();
    if (!key || !displayName) return false;
    if (this.newValueType() === 2 && !this.newAllowedValues().trim()) return false;
    return true;
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  async loadCategories(): Promise<void> {
    try {
      const cats = await this.categoryService.getCategories();
      this.categories.set(cats.filter(c => c.isActive));
    } catch {
      /* non-critical */
    }
  }

  async onCategoryChange(categoryId: string): Promise<void> {
    this.selectedCategoryId.set(categoryId);
    if (!categoryId) {
      this.attributes.set([]);
      return;
    }
    await this.loadAttributes();
  }

  async loadAttributes(): Promise<void> {
    const categoryId = this.selectedCategoryId();
    if (!categoryId) return;

    this.loading.set(true);
    try {
      const attrs = await this.categoryService.getAttributeDefinitions(
        categoryId,
        true // include inherited
      );
      this.attributes.set(attrs);
    } catch {
      this.attributes.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async onAdd(): Promise<void> {
    const categoryId = this.selectedCategoryId();
    if (!categoryId || !this.canAdd()) return;

    this.saving.set(true);
    try {
      const allowedValues = this.newValueType() === 2
        ? this.newAllowedValues().split(',').map(v => v.trim()).filter(Boolean)
        : undefined;

      const request: CreateAttributeRequest = {
        key: this.newKey().trim(),
        displayName: this.newDisplayName().trim(),
        target: this.newTarget(),
        valueType: this.newValueType(),
        isFilterable: this.newIsFilterable(),
        isRequired: this.newIsRequired(),
        sortOrder: Math.max(0, ...this.attributes().filter(a => !a.isInherited).map(a => a.sortOrder)) + 1,
        allowedValues,
        isVariantAxis: this.newIsVariantAxis(),
      };

      await this.categoryService.addAttributeDefinition(categoryId, request);
      this.toast.success(`Attribute '${request.key}' added`);
      this.resetForm();
      await this.loadAttributes();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to add attribute';
      this.toast.error(msg);
    } finally {
      this.saving.set(false);
    }
  }

  async onRemove(attr: AttributeDefinition): Promise<void> {
    const categoryId = this.selectedCategoryId();
    if (!categoryId || attr.isInherited) return;

    try {
      await this.categoryService.removeAttributeDefinition(categoryId, attr.id);
      this.toast.success(`Attribute '${attr.key}' removed`);
      await this.loadAttributes();
    } catch {
      this.toast.error('Failed to remove attribute');
    }
  }

  private resetForm(): void {
    this.newKey.set('');
    this.newDisplayName.set('');
    this.newTarget.set(1);
    this.newValueType.set(2);
    this.newAllowedValues.set('');
    this.newIsFilterable.set(true);
    this.newIsRequired.set(true);
    this.newIsVariantAxis.set(true);
  }
}
