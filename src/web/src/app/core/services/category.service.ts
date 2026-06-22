// Category service.
// Fetches categories from the Catalog API for product form dropdowns.
// Caches the result — categories rarely change during a session.
// Also manages attribute definitions on categories.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface CategoryOption {
  id: string;
  name: string;
  parentCategoryId: string | null;
  isActive: boolean;
}

export interface AttributeDefinition {
  id: string;
  key: string;
  displayName: string;
  target: 'Product' | 'Sku';
  valueType: 'Text' | 'Number' | 'Select';
  isFilterable: boolean;
  isRequired: boolean;
  sortOrder: number;
  allowedValues: string[];
  isVariantAxis: boolean;
  isInherited: boolean;
}

export interface CreateAttributeRequest {
  key: string;
  displayName: string;
  target: number;       // 0=Product, 1=Sku
  valueType: number;    // 0=Text, 1=Number, 2=Select
  isFilterable: boolean;
  isRequired: boolean;
  sortOrder: number;
  allowedValues?: string[];
  isVariantAxis?: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/catalog/categories';
  private cachedCategories: CategoryOption[] | null = null;

  async getCategories(): Promise<CategoryOption[]> {
    if (this.cachedCategories) return this.cachedCategories;

    const categories = await firstValueFrom(
      this.http.get<CategoryOption[]>(this.baseUrl)
    );
    this.cachedCategories = categories;
    return categories;
  }

  clearCache(): void {
    this.cachedCategories = null;
  }

  // ── Attribute Definitions ─────────────────────────

  async getAttributeDefinitions(
    categoryId: string,
    includeInherited = false
  ): Promise<AttributeDefinition[]> {
    const params: Record<string, string> = {};
    if (includeInherited) params['includeInherited'] = 'true';

    return firstValueFrom(
      this.http.get<AttributeDefinition[]>(
        `${this.baseUrl}/${categoryId}/attributes`,
        { params }
      )
    );
  }

  async addAttributeDefinition(
    categoryId: string,
    request: CreateAttributeRequest
  ): Promise<AttributeDefinition> {
    return firstValueFrom(
      this.http.post<AttributeDefinition>(
        `${this.baseUrl}/${categoryId}/attributes`,
        request
      )
    );
  }

  async removeAttributeDefinition(
    categoryId: string,
    attributeId: string
  ): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(
        `${this.baseUrl}/${categoryId}/attributes/${attributeId}`
      )
    );
  }
}
