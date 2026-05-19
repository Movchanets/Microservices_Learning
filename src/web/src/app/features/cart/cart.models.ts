export interface CartItem {
  sku: string;
  quantity: number;
  price: number;
  lineTotal: number;
  shopId?: string;
}

export interface ShoppingCart {
  buyerId?: string;
  items: CartItem[];
}

export interface CheckoutResponse {
  correlationId: string;
}
