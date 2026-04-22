import { Injectable, signal } from "@angular/core";
import { environment } from "../../../environments/environment";
import { Observable, tap } from "rxjs";
import { CartDto, ResponseDto } from "../../models";
import { ApiService } from "./api.service";

@Injectable({ providedIn: 'root' })
export class CartService {
    private base = environment.cartApiBase;

    // Signal for the navbar cart badge count
    cartCount = signal<number>(0);

    constructor(private api: ApiService) { }

    getCartByUserId(userId: string): Observable<ResponseDto<CartDto>> {
        return this.api.get<CartDto>(`${this.base}/api/cart/GetCart/${userId}`).pipe(
            tap(res => {
                if (res.isSuccess && res.result) {
                    this.cartCount.set(res.result.cartDetails?.length ?? 0);
                }
            })
        );
    }

    upsertCart(cart: CartDto): Observable<ResponseDto<CartDto>> {
    return this.api.post<CartDto>(`${this.base}/api/cart/CartUpsert`, cart);
  }

  removeFromCart(cartDetailsId: number): Observable<ResponseDto<unknown>> {
    return this.api.delete(`${this.base}/api/cart/RemoveCart/${cartDetailsId}`).pipe(
      tap(res => {
        if (res.isSuccess) {
          this.cartCount.update(n => Math.max(0, n - 1));
        }
      })
    );
  }

  applyCoupon(cart: CartDto): Observable<ResponseDto<unknown>> {
    return this.api.post(`${this.base}/api/cart/ApplyCoupon`, cart);
  }

  removeCoupon(cart: CartDto): Observable<ResponseDto<unknown>> {
    // Send cart with empty coupon code
    const payload: CartDto = {
      ...cart,
      cartHeader: { ...cart.cartHeader, couponCode: '' },
    };
    return this.api.post(`${this.base}/api/cart/RemoveCoupon`, payload);
  }

  emailCart(cart: CartDto): Observable<ResponseDto<unknown>> {
    return this.api.post(`${this.base}/api/cart/EmailCartRequest`, cart);
  }
}