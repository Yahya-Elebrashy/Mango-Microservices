import { ProductDto } from './product.model';

export interface CartHeaderDto {
  cartHeaderId?: number;
  userId?: string;
  couponCode?: string;
  discount?: number;
  cartTotal?: number;
  name?: string;
  phone?: string;
  email?: string;
}

export interface CartDetailsDto {
  cartDetailsId?: number;
  cartHeaderId?: number;
  cartHeader?: CartHeaderDto;
  productId: number;
  productDto?: ProductDto;
  count: number;
}

export interface CartDto {
  cartHeader: CartHeaderDto;
  cartDetails?: CartDetailsDto[];
}