import { Component, computed, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CartDetailsDto, CartDto, CartHeaderDto, ProductDto } from '../../../../models';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../../core/services/product.service';
import { CartService } from '../../../../core/services/cart.service';
import { AuthState } from '../../../../core/services/auth-state.service';
import { ToastrService } from 'ngx-toastr';
import { Alert } from '../../../../shared/components/alert/alert';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-detail',
  imports: [CommonModule, FormsModule, RouterLink, Spinner, Alert],
  templateUrl: './detail.html',
  styleUrl: './detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Detail implements OnInit {

  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private productService = inject(ProductService);
  private cartService    = inject(CartService);
  private authState      = inject(AuthState);
  private toastr         = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  product      = signal<ProductDto | null>(null);
  count        = signal(1);
  loading      = signal(false);
  addingToCart = signal(false);
  errorMsg     = signal('');

  // ── Derived signals ────────────────────────────────────────────
  canDecrement = computed(() => this.count() > 1);

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.router.navigate(['/']);
      return;
    }
    this.loadProduct(id);
  }

  // ── Methods ────────────────────────────────────────────────────
  loadProduct(id: number): void {
    this.loading.set(true);
    this.errorMsg.set('');

    this.productService.getById(id).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.product.set(res.result);
        } else {
          this.errorMsg.set(res.message || 'Product not found.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to load product.');
      },
    });
  }

  increment(): void {
    this.count.update(c => c + 1);
  }

  decrement(): void {
    if (this.canDecrement()) {
      this.count.update(c => c - 1);
    }
  }

  addToCart(): void {
    const currentProduct = this.product();
    if (!currentProduct) return;

    const userId = this.authState.currentUser?.id;
    if (!userId) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const cart: CartDto = {
      cartHeader: { userId } as CartHeaderDto,
      cartDetails: [
        {
          productId: currentProduct.productId,
          count: this.count(),
        } as CartDetailsDto,
      ],
    };

    this.addingToCart.set(true);

    this.cartService.upsertCart(cart).subscribe({
      next: res => {
        this.addingToCart.set(false);
        if (res.isSuccess) {
          this.cartService.getCartByUserId(userId).subscribe();
          this.toastr.success(`${currentProduct.name} added to cart!`);
          this.router.navigate(['/']);
        } else {
          this.toastr.error(res.message || 'Could not add to cart.');
        }
      },
      error: () => {
        this.addingToCart.set(false);
      },
    });
  }
}