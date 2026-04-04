import { OrderStatus } from '../constants/app.constants';

export interface OrderDetailsDto {
  orderDetailsId: number;
  orderHeaderId: number;
  productId: number;
  productName: string;
  price: number;
  count: number;
}

export interface OrderHeaderDto {
  orderHeaderId: number;
  userId?: string;
  couponCode?: string;
  discount?: number;
  orderTotal: number;
  name: string;
  phone: string;
  email: string;
  orderTime: string;
  status: OrderStatus;
  paymentIntentId?: string;
  stripeSessionId?: string;
  orderDetails?: OrderDetailsDto[];
}

export interface StripeRequestDto {
  stripeSessionUrl?: string;
  stripeSessionId?: string;
  approvedUrl: string;
  cancelUrl: string;
  orderHeader: OrderHeaderDto;
}