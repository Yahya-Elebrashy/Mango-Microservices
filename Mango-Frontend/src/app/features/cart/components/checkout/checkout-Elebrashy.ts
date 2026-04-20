import { CommonModule } from '@angular/common';
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { CartDto, StripeRequestDto } from '../../../../models';
import { CartService } from '../../../../core/services/cart.service';
import { OrderService } from '../../../../core/services/order.service';
import { ToastrService } from 'ngx-toastr';
import { AuthState } from '../../../../core/services/auth-state.service';

@Component({
  selector: 'app-checkout',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, Spinner],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Checkout implements OnInit {

  private fb           = inject(FormBuilder);
  private cartService  = inject(CartService);
  private orderService = inject(OrderService);
  private authState    = inject(AuthState);
  private toastr       = inject(ToastrService);
  private router       = inject(Router);

  // ── State signals ──────────────────────────────────────────────
  cart       = signal<CartDto | null>(null);
  loading    = signal(false);
  submitting = signal(false);

  // ── Derived signals ────────────────────────────────────────────
  cartTotal = computed(() => this.cart()?.cartHeader.cartTotal ?? 0);
  itemCount = computed(() => this.cart()?.cartDetails?.length  ?? 0);

  // ── Form (ReactiveFormsModule owns its own reactivity) ─────────
  form: FormGroup = this.fb.group({
    name:  [this.authState.currentUser?.name        ?? '', Validators.required],
    email: [this.authState.currentUser?.email       ?? '', [Validators.required, Validators.email]],
    phone: [this.authState.currentUser?.phoneNumber ?? '', Validators.required],
  });

  get f() { return this.form.controls; }

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    const userId = this.authState.currentUser?.id;
    if (!userId) return;

    this.loading.set(true);

    this.cartService.getCartByUserId(userId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.cart.set(res.result);
        } else {
          this.toastr.error('Could not load cart.');
          this.router.navigate(['/cart']);
        }
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/cart']);
      },
    });
  }

  // ── Methods ────────────────────────────────────────────────────
  onSubmit(): void {
    const currentCart = this.cart();
    if (this.form.invalid || !currentCart) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    // Merge contact details into cart header
    const enrichedCart: CartDto = {
      ...currentCart,
      cartHeader: {
        ...currentCart.cartHeader,
        name:  this.form.value.name,
        email: this.form.value.email,
        phone: this.form.value.phone,
      },
    };

    // Step 1 — Create order
    this.orderService.createOrder(enrichedCart).subscribe({
      next: orderRes => {
        if (!orderRes.isSuccess || !orderRes.result) {
          this.submitting.set(false);
          this.toastr.error(orderRes.message || 'Failed to create order.');
          return;
        }

        const orderHeader = orderRes.result;

        // Step 2 — Create Stripe session
        const stripeRequest: StripeRequestDto = {
          approvedUrl: `${window.location.origin}/cart/confirmation?orderId=${orderHeader.orderHeaderId}`,
          cancelUrl:   `${window.location.origin}/cart/checkout`,
          orderHeader,
        };

        this.orderService.createStripeSession(stripeRequest).subscribe({
          next: stripeRes => {
            this.submitting.set(false);
            if (stripeRes.isSuccess && stripeRes.result?.stripeSessionUrl) {
              console.log(stripeRes.result.stripeSessionUrl);
              window.location.href = stripeRes.result.stripeSessionUrl;
            } else {
              this.toastr.error('Could not initiate payment.');
            }
          },
          error: () => this.submitting.set(false),
        });
      },
      error: () => this.submitting.set(false),
    });
  }
}