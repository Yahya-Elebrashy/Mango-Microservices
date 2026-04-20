import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductService } from '../../../../core/services/product.service';
import { ToastrService } from 'ngx-toastr';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Spinner } from '../../../../shared/components/spinner/spinner';

@Component({
  selector: 'app-product-create',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './product-create.html',
  styleUrl: './product-create.scss',
})
export class ProductCreate {

  form: FormGroup;
  loading = false;
  imagePreview: string | null = null;
  imageError = '';

  readonly MAX_SIZE_MB = 1;
  readonly ALLOWED_TYPES = ['image/jpeg', 'image/png'];

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private toastr: ToastrService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      name:         ['', [Validators.required, Validators.minLength(2)]],
      price:        [null, [Validators.required, Validators.min(0.01)]],
      description:  ['', [Validators.required, Validators.minLength(10)]],
      categoryName: ['', Validators.required],
      image:        [null],
    });
  }

  get f() { return this.form.controls; }

  onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    this.imageError = '';
    this.imagePreview = null;

    if (!file) return;

    // Validate type
    if (!this.ALLOWED_TYPES.includes(file.type)) {
      this.imageError = 'Only JPG and PNG files are allowed.';
      return;
    }

    // Validate size (MaxFileSizeAttribute equivalent)
    if (file.size > this.MAX_SIZE_MB * 1024 * 1024) {
      this.imageError = `File must be under ${this.MAX_SIZE_MB}MB.`;
      return;
    }

    this.form.patchValue({ image: file });

    // Preview
    const reader = new FileReader();
    reader.onload = () => (this.imagePreview = reader.result as string);
    reader.readAsDataURL(file);
  }

   onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.imageError) return;

    this.loading = true;

    this.productService.create(this.form.value).subscribe({
      next: res => {
        this.loading = false;
        if (res.isSuccess) {
          this.toastr.success('Product created successfully.');
          this.router.navigate(['/product']);
        } else {
          this.toastr.error(res.message || 'Failed to create product.');
        }
      },
      error: () => { this.loading = false; },
    });
  }

}
