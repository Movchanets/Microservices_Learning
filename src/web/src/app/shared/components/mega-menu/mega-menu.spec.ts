import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MegaMenu } from './mega-menu';
import { CategoryTreeService, CategoryTree } from '../../../core/services/category-tree.service';
import { Router } from '@angular/router';
import { Component, importProvidersFrom, signal } from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { LucideAngularModule, ChevronRight } from 'lucide-angular';

@Component({ template: '' })
class DummyComponent {}

const routes: Routes = [
  { path: 'catalog', component: DummyComponent },
];

const mockCategories: CategoryTree[] = [
  {
    id: '1', name: 'Electronics', description: null, parentCategoryId: null,
    slug: 'electronics', sortOrder: 1, isActive: true,
    children: [
      {
        id: '11', name: 'Phones', description: null, parentCategoryId: '1',
        slug: 'phones', sortOrder: 1, isActive: true,
        children: [
          {
            id: '111', name: 'Smartphones', description: null, parentCategoryId: '11',
            slug: 'smartphones', sortOrder: 1, isActive: true, children: [],
          },
        ],
      },
      {
        id: '12', name: 'Laptops', description: null, parentCategoryId: '1',
        slug: 'laptops', sortOrder: 2, isActive: true, children: [],
      },
    ],
  },
  {
    id: '2', name: 'Clothing', description: null, parentCategoryId: null,
    slug: 'clothing', sortOrder: 2, isActive: true,
    children: [
      {
        id: '21', name: 'Men', description: null, parentCategoryId: '2',
        slug: 'men', sortOrder: 1, isActive: true, children: [],
      },
    ],
  },
];

describe('MegaMenu', () => {
  let component: MegaMenu;
  let fixture: ComponentFixture<MegaMenu>;
  let mockCategoryTreeService: { categoryTree: ReturnType<typeof signal<CategoryTree[]>> };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockCategoryTreeService = {
      categoryTree: signal<CategoryTree[]>(mockCategories),
    };
    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [MegaMenu],
      providers: [
        { provide: CategoryTreeService, useValue: mockCategoryTreeService },
        { provide: Router, useValue: mockRouter },
        importProvidersFrom(LucideAngularModule.pick({ ChevronRight })),
        provideRouter(routes),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MegaMenu);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders root categories in the left column', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = compiled.querySelectorAll('.w-1\\/4 button');
    expect(buttons.length).toBe(2);
    expect(buttons[0].textContent).toContain('Electronics');
    expect(buttons[1].textContent).toContain('Clothing');
  });

  it('auto-selects the first root category on init', () => {
    expect(component.activeRoot()?.id).toBe('1');
    expect(component.activeRoot()?.name).toBe('Electronics');
  });

  it('displays subcategories of the active root', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const rightPanel = compiled.querySelector('.flex-1.p-8');
    expect(rightPanel?.textContent).toContain('Phones');
    expect(rightPanel?.textContent).toContain('Laptops');
  });

  it('switches active root on mouseenter', () => {
    component.activeRoot.set(mockCategories[1]);
    fixture.detectChanges();

    expect(component.activeRoot()?.name).toBe('Clothing');

    const compiled = fixture.nativeElement as HTMLElement;
    const rightPanel = compiled.querySelector('.flex-1.p-8');
    expect(rightPanel?.textContent).toContain('Men');
  });

  it('navigates to catalog with categoryId on category click', () => {
    const childCategory = mockCategories[0].children[0]; // Phones
    component.onCategoryClick(childCategory);

    expect(mockRouter.navigate).toHaveBeenCalledWith(
      ['/catalog'],
      { queryParams: { categoryId: '11' } }
    );
  });

  it('emits closeMenu on category click', () => {
    const spy = vi.fn();
    component.closeMenu.subscribe(spy);

    component.onCategoryClick(mockCategories[0]);
    expect(spy).toHaveBeenCalledOnce();
  });

  it('shows empty state when no categories exist', () => {
    mockCategoryTreeService.categoryTree.set([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No categories found');
  });

  it('renders grandchild categories', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const rightPanel = compiled.querySelector('.flex-1.p-8');
    expect(rightPanel?.textContent).toContain('Smartphones');
  });
});
