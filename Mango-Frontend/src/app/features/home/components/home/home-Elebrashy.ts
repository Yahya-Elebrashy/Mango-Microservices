import { Component, computed, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ProductDto } from '../../../../models';
import { ProductService } from '../../../../core/services/product.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { Alert } from '../../../../shared/components/alert/alert';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink, Spinner, Alert],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home implements OnInit {

  private productService = inject(ProductService);

  // ── State signals ──────────────────────────────────────────────
  products      = signal<ProductDto[]>([]);
  loading       = signal(false);
  errorMsg      = signal('');
  searchTerm    = signal('');
  selectedCategory = signal('');

  // ── Derived (computed) signals ─────────────────────────────────
  categories = computed(() =>
    [...new Set(this.products().map(p => p.categoryName))]
  );

  filtered = computed(() => {
    const term = this.searchTerm().toLowerCase();
    const cat  = this.selectedCategory();

    return this.products().filter(p =>
      p.name.toLowerCase().includes(term) &&
      (cat ? p.categoryName === cat : true)
    );
  });

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadProducts();
  }

  // ── Methods ────────────────────────────────────────────────────
  loadProducts(): void {
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

  onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  onCategoryChange(cat: string): void {
    this.selectedCategory.set(cat);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedCategory.set('');
  }
}