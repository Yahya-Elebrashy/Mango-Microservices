import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CouponDto } from '../../../../models';
import { CouponService } from '../../../../core/services/coupon.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-coupon-delete',
  imports: [CommonModule, RouterLink, Spinner],
  templateUrl: './coupon-delete.html',
  styleUrl: './coupon-delete.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CouponDelete implements OnInit {

  private route         = inject(ActivatedRoute);
  private router        = inject(Router);
  private couponService = inject(CouponService);
  private toastr        = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  coupon   = signal<CouponDto | null>(null);
  loading  = signal(false);
  deleting = signal(false);

  readonly couponId = Number(this.route.snapshot.paramMap.get('id'));

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    if (!this.couponId) {
      this.router.navigate(['/coupon']);
      return;
    }
    this.load();
  }

  // ── Methods ────────────────────────────────────────────────────
  load(): void {
    this.loading.set(true);

    this.couponService.getById(this.couponId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.coupon.set(res.result);
        } else {
          this.toastr.error('Coupon not found.');
          this.router.navigate(['/coupon']);
        }
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/coupon']);
      },
    });
  }

  confirmDelete(): void {
    this.deleting.set(true);

    this.couponService.delete(this.couponId).subscribe({
      next: res => {
        this.deleting.set(false);
        if (res.isSuccess) {
          this.toastr.success('Coupon deleted.');
          this.router.navigate(['/coupon']);
        } else {
          this.toastr.error(res.message || 'Failed to delete coupon.');
        }
      },
      error: () => this.deleting.set(false),
    });
  }
}