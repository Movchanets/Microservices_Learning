export interface CartItem {
  sku: string;
  quantity: number;
  unitPrice?: number;
  sellerId?: string;
}

export interface ShoppingCart {
  buyerId?: string;
  items: CartItem[];
}

export interface CheckoutResponse {
  correlationId: string;
}
