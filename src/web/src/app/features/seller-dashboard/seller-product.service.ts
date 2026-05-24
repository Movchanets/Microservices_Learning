// Seller product service.
// Handles CRUD operations for seller products via the Catalog API.
// Uses the BFF pattern - all calls go through /api/catalog/products.

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom, map } from 'rxjs';
import { SellerProduct, CreateProductRequest, UpdateProductRequest } from './seller.models';
import { PagedResult } from '../catalog/catalog.models';

@Injectable({ providedIn: 'root' })
export class SellerProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/catalog/products';

  async getMyProducts(storeId: string): Promise<SellerProduct[]> {
    const params = new HttpParams().set('storeId', storeId);
    return firstValueFrom(
      this.http.get<PagedResult<SellerProduct>>(this.baseUrl, { params })
        .pipe(map(res => res.items))
    );
  }

  async getProductById(id: string): Promise<SellerProduct> {
    return firstValueFrom(
      this.http.get<SellerProduct>(`${this.baseUrl}/${id}`)
    );
  }

  async createProduct(request: CreateProductRequest): Promise<SellerProduct> {
    return firstValueFrom(
      this.http.post<SellerProduct>(this.baseUrl, request)
    );
  }

  async updateProduct(id: string, request: UpdateProductRequest): Promise<SellerProduct> {
    return firstValueFrom(
      this.http.put<SellerProduct>(`${this.baseUrl}/${id}`, request)
    );
  }

  async changePrice(id: string, price: number, currency: string): Promise<void> {
    return firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/${id}/price`, { price, currency })
    );
  }

  async activateProduct(id: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.baseUrl}/${id}/activate`, {})
    );
  }

  async deactivateProduct(id: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.baseUrl}/${id}/deactivate`, {})
    );
  }

  async deleteProduct(id: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`)
    );
  }
}
