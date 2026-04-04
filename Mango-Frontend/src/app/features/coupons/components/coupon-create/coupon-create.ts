import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CouponService } from '../../../../core/services/coupon.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-coupon-create',
  imports: [ CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './coupon-create.html',
  styleUrl: './coupon-create.scss',
})
export class CouponCreate {
  form: FormGroup;
  loading = false;

  constructor(
    private fb: FormBuilder,
    private couponService: CouponService,
    private toastr: ToastrService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      couponCode: [
        '',
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(20),
          Validators.pattern(/^[A-Z0-9_-]+$/),   // uppercase alphanumeric
        ],
      ],
      discountAmount: [
        null,
        [Validators.required, Validators.min(0.01)],
      ],
      minAmount: [
        0,
        [Validators.required, Validators.min(0)],
      ],
    });
  }

  get f() { return this.form.controls; }

  // Auto-uppercase the coupon code as the user types
  onCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const upper = input.value.toUpperCase();
    this.form.patchValue({ couponCode: upper }, { emitEvent: false });
    input.value = upper;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;

    this.couponService.create(this.form.value).subscribe({
      next: res => {
        this.loading = false;
        if (res.isSuccess) {
          this.toastr.success('Coupon created successfully.');
          this.router.navigate(['/coupon']);
        } else {
          this.toastr.error(res.message || 'Failed to create coupon.');
        }
      },
      error: () => { this.loading = false; },
    });
  }
}
