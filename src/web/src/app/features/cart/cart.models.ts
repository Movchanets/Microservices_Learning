export interface CartItem {
  productId: string;
  storeId: string;
  quantity: number;
  price: number;
  lineTotal: number;
}

export interface ShoppingCart {
  buyerId?: string;
  items: CartItem[];
}

export interface CheckoutResponse {
  correlationId: string;
}
