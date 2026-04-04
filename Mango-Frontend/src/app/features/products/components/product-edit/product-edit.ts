import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../../core/services/product.service';
import { ToastrService } from 'ngx-toastr';
import { Spinner } from '../../../../shared/components/spinner/spinner';

@Component({
  selector: 'app-product-edit',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, Spinner],
  templateUrl: './product-edit.html',
  styleUrl: './product-edit.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductEdit implements OnInit {

  private fb             = inject(FormBuilder);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private productService = inject(ProductService);
  private toastr         = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  loading      = signal(false);
  saving       = signal(false);
  imagePreview = signal<string | null>(null);
  imageError   = signal('');

  // ── Constants ──────────────────────────────────────────────────
  readonly MAX_SIZE_MB   = 1;
  readonly ALLOWED_TYPES = ['image/jpeg', 'image/png'];

  // ── Form (not a signal — ReactiveFormsModule owns its state) ───
  readonly productId = Number(this.route.snapshot.paramMap.get('id'));

  form: FormGroup = this.fb.group({
    productId:    [this.productId],
    name:         ['', [Validators.required, Validators.minLength(2)]],
    price:        [null, [Validators.required, Validators.min(0.01)]],
    description:  ['', [Validators.required, Validators.minLength(10)]],
    categoryName: ['', Validators.required],
    imageUrl:     [''],
    image:        [null],
  });

  get f() { return this.form.controls; }

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
          this.form.patchValue(res.result);
          this.imagePreview.set(res.result.imageUrl ?? null);
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
  onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    this.imageError.set('');

    if (!file) return;

    if (!this.ALLOWED_TYPES.includes(file.type)) {
      this.imageError.set('Only JPG and PNG files are allowed.');
      return;
    }

    if (file.size > this.MAX_SIZE_MB * 1024 * 1024) {
      this.imageError.set(`File must be under ${this.MAX_SIZE_MB}MB.`);
      return;
    }

    this.form.patchValue({ image: file });

    const reader = new FileReader();
    reader.onload = () => this.imagePreview.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.imageError()) return;

    this.saving.set(true);

    this.productService.update(this.form.value).subscribe({
      next: res => {
        this.saving.set(false);
        if (res.isSuccess) {
          this.toastr.success('Product updated successfully.');
          this.router.navigate(['/product']);
        } else {
          this.toastr.error(res.message || 'Failed to update product.');
        }
      },
      error: () => this.saving.set(false),
    });
  }
}