import { CommonModule } from '@angular/common';
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartDto } from '../../../../models';
import { CartService } from '../../../../core/services/cart.service';
import { AuthState } from '../../../../core/services/auth-state.service';
import { ToastrService } from 'ngx-toastr';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { Alert } from '../../../../shared/components/alert/alert';

@Component({
  selector: 'app-cart-index',
  imports: [CommonModule, RouterLink, FormsModule, Spinner, Alert],
  templateUrl: './cart-index.html',
  styleUrl: './cart-index.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CartIndex implements OnInit {

  private cartService = inject(CartService);
  private toastr = inject(ToastrService);
  authState = inject(AuthState);

  // ── State signals ──────────────────────────────────────────────
  cart = signal<CartDto | null>(null);
  loading = signal(false);
  errorMsg = signal('');
  couponCode = signal('');
  applyingCoupon = signal(false);
  removingCoupon = signal(false);
  removingItemId = signal<number | null>(null);
  emailingCart = signal(false);

  // ── Derived signals ────────────────────────────────────────────
  cartTotal = computed(() => this.cart()?.cartHeader.cartTotal ?? 0);
  discount = computed(() => this.cart()?.cartHeader.discount ?? 0);
  itemCount = computed(() => this.cart()?.cartDetails?.length ?? 0);

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadCart();
  }

  // ── Methods ────────────────────────────────────────────────────
  loadCart(): void {
    const userId = this.authState.currentUser?.id;
    if (!userId) return;

    this.loading.set(true);

    this.cartService.getCartByUserId(userId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.cart.set(res.result);
          this.couponCode.set(res.result.cartHeader.couponCode ?? '');
        } else {
          this.cart.set(null);
          this.errorMsg.set(res.message || 'Could not load cart.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to reach the cart service.');
      },
    });
  }

  removeItem(cartDetailsId: number): void {
    this.removingItemId.set(cartDetailsId);

    this.cartService.removeFromCart(cartDetailsId).subscribe({
      next: res => {
        this.removingItemId.set(null);
        if (res.isSuccess) {
          this.toastr.success('Item removed.');
          this.loadCart();
        } else {
          this.toastr.error(res.message || 'Could not remove item.');
        }
      },
      error: () => this.removingItemId.set(null),
    });
  }

  applyCoupon(): void {
    const currentCart = this.cart();
    if (!currentCart || !this.couponCode().trim()) return;

    this.applyingCoupon.set(true);
    const payload: CartDto = {
      ...currentCart,
      cartHeader: { ...currentCart.cartHeader, couponCode: this.couponCode().trim() },
    };

    this.cartService.applyCoupon(payload).subscribe({
      next: res => {
        this.applyingCoupon.set(false);
        if (res.isSuccess) {
          this.toastr.success('Coupon applied.');
          this.loadCart();
        } else {
          this.toastr.error(res.message || 'Invalid coupon code.');
        }
      },
      error: () => this.applyingCoupon.set(false),
    });
  }

  removeCoupon(): void {
    const currentCart = this.cart();
    if (!currentCart) return;

    this.removingCoupon.set(true);

    this.cartService.removeCoupon(currentCart).subscribe({
      next: res => {
        this.removingCoupon.set(false);
        if (res.isSuccess) {
          this.toastr.success('Coupon removed.');
          this.couponCode.set('');
          this.loadCart();
        } else {
          this.toastr.error(res.message || 'Could not remove coupon.');
        }
      },
      error: () => this.removingCoupon.set(false),
    });
  }

  emailCart(): void {
    const currentCart = this.cart();
    if (!currentCart) return;

    const email = this.authState.currentUser?.email;
    if (!email) return;

    this.emailingCart.set(true);
    const payload: CartDto = {
      ...currentCart,
      cartHeader: { ...currentCart.cartHeader, email },
    };

    this.cartService.emailCart(payload).subscribe({
      next: res => {
        this.emailingCart.set(false);
        if (res.isSuccess) {
          this.toastr.success('Cart emailed successfully!');
        } else {
          this.toastr.error('Failed to send email.');
        }
      },
      error: () => this.emailingCart.set(false),
    });
  }
}