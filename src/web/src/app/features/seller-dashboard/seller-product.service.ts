// Seller product service.
// Handles CRUD operations for seller products via the Catalog API.
// Uses the BFF pattern - all calls go through /api/catalog/products.

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SellerProduct, CreateProductRequest, UpdateProductRequest } from './seller.models';

@Injectable({ providedIn: 'root' })
export class SellerProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/catalog/products';

  async getMyProducts(sellerId: string): Promise<SellerProduct[]> {
    return firstValueFrom(
      this.http.get<SellerProduct[]>(`${this.baseUrl}?sellerId=${sellerId}`)
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

  async deleteProduct(id: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`)
    );
  }
}
