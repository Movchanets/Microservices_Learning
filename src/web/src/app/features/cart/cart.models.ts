export interface CartItemDetails {
  productId: string;
  skuId: string;
  skuCode: string;
  title: string;
  imageUrl: string | null;
  quantity: number;
  price: number;
  lineTotal: number;
  storeId: string;
}

export interface ShoppingCart {
  buyerId: string | null;
  cartId: string;
  items: CartItemDetails[];
  totalPrice: number;
  totalItems: number;
}

export interface CheckoutResponse {
  correlationId: string;
}
