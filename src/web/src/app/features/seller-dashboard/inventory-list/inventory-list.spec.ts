// InventoryListComponent unit tests.
// Tests inventory list rendering, filtering, stock status labels, and add stock flow.

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { importProvidersFrom, signal } from '@angular/core';
import { LucideAngularModule, Package, AlertTriangle, Plus, Loader } from 'lucide-angular';
import { InventoryListComponent } from './inventory-list';
import { InventoryStore, InventoryDisplayItem } from '../inventory.store';
import { ToastService } from '../../../core/services/toast.service';

describe('InventoryListComponent', () => {
  let component: InventoryListComponent;
  let fixture: ComponentFixture<InventoryListComponent>;

  const mockItems: InventoryDisplayItem[] = [
    { sku: 'WP-1', productName: 'Widget Pro', imageUrl: null, quantity: 15, status: 'in-stock', lastUpdated: '2026-01-01' },
    { sku: 'GM-1', productName: 'Gadget Mini', imageUrl: null, quantity: 3, status: 'low-stock', lastUpdated: '2026-01-01' },
    { sku: 'TM-1', productName: 'Thing Max', imageUrl: null, quantity: 0, status: 'out-of-stock', lastUpdated: '2026-01-01' },
  ];

  const mockStore = {
    items: signal<InventoryDisplayItem[]>(mockItems),
    loading: signal(false),
    error: signal<string | null>(null),
    lowStockCount: signal(2),
    loadInventory: vi.fn(),
    addStock: vi.fn().mockResolvedValue(true),
  };

  const mockToast = {
    success: vi.fn(),
    error: vi.fn(),
  };

  beforeEach(async () => {
    // Reset store state
    mockStore.items.set(mockItems);
    mockStore.loading.set(false);
    mockStore.error.set(null);
    mockStore.lowStockCount.set(2);

    await TestBed.configureTestingModule({
      imports: [
        LucideAngularModule.pick({ Package, AlertTriangle, Plus, Loader }),
        InventoryListComponent,
      ],
      providers: [
        { provide: InventoryStore, useValue: mockStore },
        { provide: ToastService, useValue: mockToast },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryListComponent);
    component = fixture.componentInstance;
    vi.clearAllMocks();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load inventory on init', () => {
    expect(mockStore.loadInventory).toHaveBeenCalled();
  });

  it('should show all items by default', () => {
    expect(component.filteredItems()).toHaveLength(3);
  });

  it('should filter by low-stock', () => {
    component.filter.set('low-stock');
    expect(component.filteredItems()).toHaveLength(1);
    expect(component.filteredItems()[0].sku).toBe('GM-1');
  });

  it('should filter by out-of-stock', () => {
    component.filter.set('out-of-stock');
    expect(component.filteredItems()).toHaveLength(1);
    expect(component.filteredItems()[0].sku).toBe('TM-1');
  });

  it('should show loading state', () => {
    mockStore.loading.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.animate-pulse')).toBeTruthy();
  });

  it('should show error state', () => {
    mockStore.loading.set(false);
    mockStore.error.set('Failed to load inventory');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Failed to load inventory');
  });

  describe('statusLabel', () => {
    it('should return In Stock for in-stock', () => {
      expect(component.statusLabel('in-stock')).toBe('In Stock');
    });

    it('should return Low Stock for low-stock', () => {
      expect(component.statusLabel('low-stock')).toBe('Low Stock');
    });

    it('should return Out of Stock for out-of-stock', () => {
      expect(component.statusLabel('out-of-stock')).toBe('Out of Stock');
    });
  });

  describe('statusClass', () => {
    it('should return green for in-stock', () => {
      expect(component.statusClass('in-stock')).toContain('green');
    });

    it('should return orange for low-stock', () => {
      expect(component.statusClass('low-stock')).toContain('orange');
    });

    it('should return red for out-of-stock', () => {
      expect(component.statusClass('out-of-stock')).toContain('red');
    });
  });

  describe('getCount', () => {
    it('should count items by status', () => {
      expect(component.getCount('in-stock')).toBe(1);
      expect(component.getCount('low-stock')).toBe(1);
      expect(component.getCount('out-of-stock')).toBe(1);
    });

    it('should return 0 for unknown status', () => {
      expect(component.getCount('unknown')).toBe(0);
    });
  });

  describe('confirmAddStock', () => {
    it('should call store.addStock and show success toast', async () => {
      component.addQuantity = 5;
      await component.confirmAddStock('WP-1');

      expect(mockStore.addStock).toHaveBeenCalledWith('WP-1', 5);
      expect(mockToast.success).toHaveBeenCalledWith('Added 5 units to WP-1');
    });

    it('should not call store.addStock when quantity is 0', async () => {
      component.addQuantity = 0;
      await component.confirmAddStock('WP-1');

      expect(mockStore.addStock).not.toHaveBeenCalled();
    });

    it('should show error toast on failure', async () => {
      mockStore.addStock.mockResolvedValueOnce(false);
      component.addQuantity = 5;

      await component.confirmAddStock('WP-1');

      expect(mockToast.error).toHaveBeenCalledWith('Failed to add stock');
    });

    it('should reset form after success', async () => {
      component.addQuantity = 5;
      component.addingToSku.set('WP-1');

      await component.confirmAddStock('WP-1');

      expect(component.addingToSku()).toBeNull();
      expect(component.addQuantity).toBe(1);
    });
  });
});
