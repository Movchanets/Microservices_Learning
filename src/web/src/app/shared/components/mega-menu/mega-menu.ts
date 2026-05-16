import { Component, EventEmitter, Output, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LucideAngularModule, ChevronRight } from 'lucide-angular';
import { CategoryTree, CategoryTreeService } from '../../../core/services/category-tree.service';

@Component({
  selector: 'app-mega-menu',
  imports: [CommonModule, LucideAngularModule],
  standalone: true,
  templateUrl: './mega-menu.html',
})
export class MegaMenu {
  private categoryTreeService = inject(CategoryTreeService);
  private router = inject(Router);

  @Output() closeMenu = new EventEmitter<void>();

  tree = this.categoryTreeService.categoryTree;
  activeRoot = signal<CategoryTree | null>(null);
  
  readonly ChevronRightIcon = ChevronRight;

  constructor() {
    effect(() => {
      const currentTree = this.tree();
      if (currentTree.length > 0 && !this.activeRoot()) {
        this.activeRoot.set(currentTree[0]);
      }
    });
  }

  onCategoryClick(category: CategoryTree) {
    this.closeMenu.emit();
    this.router.navigate(['/catalog'], { queryParams: { categoryId: category.id } });
  }
}
