import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ProductDto } from '../../../../models';
import { ProductService } from '../../../../core/services/product.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { Alert } from '../../../../shared/components/alert/alert';

@Component({
  selector: 'app-product-list',
  imports: [CommonModule, RouterLink, Spinner, Alert],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList implements OnInit {

  private productService = inject(ProductService);
  private toastr         = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  products = signal<ProductDto[]>([]);
  loading  = signal(false);
  errorMsg = signal('');

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    this.load();
  }

  // ── Methods ────────────────────────────────────────────────────
  load(): void {
    this.loading.set(true);
    this.errorMsg.set('');

    this.productService.getAll().subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.products.set(res.result);
        } else {
          this.errorMsg.set(res.message || 'Failed to load products.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to reach the product service.');
      },
    });
  }
}