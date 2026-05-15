export type OrderStatus =
  | 'Submitted'
  | 'InventoryReserved'
  | 'PaymentProcessing'
  | 'Completed'
  | 'Cancelled'
  | 'Faulted';

export interface OrderItem {
  id: string;
  sku: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface Order {
  id: string;
  buyerId: string;
  status: OrderStatus;
  totalAmount: number;
  createdAt: string;
  completedAt: string | null;
  items: OrderItem[];
}

export interface PaymentStatus {
  id: string;
  orderId: string;
  amount: number;
  status: 'Pending' | 'Completed' | 'Failed' | 'Refunded';
  transactionId: string | null;
  failureReason: string | null;
  createdAt: string;
  processedAt: string | null;
}
