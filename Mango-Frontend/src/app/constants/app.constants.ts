export const SD = {
  // Roles
  ROLE_ADMIN: 'ADMIN',
  ROLE_CUSTOMER: 'CUSTOMER',

  // Token
  TOKEN_KEY: 'JwtToken',

  // Order statuses
  STATUS_PENDING: 'Pending',
  STATUS_APPROVED: 'Approved',
  STATUS_READY_FOR_PICKUP: 'ReadyForPickup',
  STATUS_COMPLETED: 'Completed',
  STATUS_REFUNDED: 'Refunded',
  STATUS_CANCELLED: 'Cancelled',

  // API base URLs — overridden per environment
  COUPON_API: '',
  AUTH_API: '',
  PRODUCT_API: '',
  CART_API: '',
  ORDER_API: '',
} as const;

export type OrderStatus =
  | 'Pending'
  | 'Approved'
  | 'ReadyForPickup'
  | 'Completed'
  | 'Refunded'
  | 'Cancelled';

export type UserRole = 'ADMIN' | 'CUSTOMER';