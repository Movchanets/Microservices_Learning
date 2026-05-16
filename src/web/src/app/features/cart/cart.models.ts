export interface CartItem {
  sku: string;
  quantity: number;
  unitPrice?: number;
}

export interface ShoppingCart {
  buyerId?: string;
  items: CartItem[];
}

export interface CheckoutResponse {
  correlationId: string;
}
