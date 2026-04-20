import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ProductDto } from '../../../../models';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../../core/services/product.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { Spinner } from '../../../../shared/components/spinner/spinner';

@Component({
  selector: 'app-product-delete',
  imports: [CommonModule, RouterLink, Spinner],
  templateUrl: './product-delete.html',
  styleUrl: './product-delete.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDelete implements OnInit {

  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private productService = inject(ProductService);
  private toastr         = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  product  = signal<ProductDto | null>(null);
  loading  = signal(false);
  deleting = signal(false);

  readonly productId = Number(this.route.snapshot.paramMap.get('id'));

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    if (!this.productId) {
      this.router.navigate(['/product']);
      return;
    }

    this.loading.set(true);

    this.productService.getById(this.productId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.product.set(res.result);
        } else {
          this.toastr.error('Product not found.');
          this.router.navigate(['/product']);
        }
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/product']);
      },
    });
  }

  // ── Methods ────────────────────────────────────────────────────
  confirmDelete(): void {
    this.deleting.set(true);

    this.productService.delete(this.productId).subscribe({
      next: res => {
        this.deleting.set(false);
        if (res.isSuccess) {
          this.toastr.success('Product deleted.');
          this.router.navigate(['/product']);
        } else {
          this.toastr.error(res.message || 'Failed to delete product.');
        }
      },
      error: () => this.deleting.set(false),
    });
  }
}