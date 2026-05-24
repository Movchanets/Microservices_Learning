// Admin panel data models.
// Defines types for user management, store verification, and admin stats.

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Buyer' | 'Seller' | 'Admin';
  createdAt: string;
  isActive: boolean;
}

export interface UpdateUserRoleRequest {
  role: 'Buyer' | 'Seller' | 'Admin';
}

export interface AdminStore {
  id: string;
  sellerId: string;
  name: string;
  description: string;
  logoUrl: string | null;
  verificationStatus: 'Pending' | 'Verified' | 'Rejected';
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string | null;
  verifiedAt: string | null;
}

export interface VerifyStoreRequest {
  isApproved: boolean;
  reason?: string;
}

export interface AdminStats {
  totalUsers: number;
  totalStores: number;
  pendingVerifications: number;
  totalOrders: number;
}
