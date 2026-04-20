import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CouponDto } from '../../../../models';
import { CouponService } from '../../../../core/services/coupon.service';
import { ToastrService } from 'ngx-toastr';
import { Alert } from '../../../../shared/components/alert/alert';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-coupon-list',
  imports: [CommonModule, RouterLink, Spinner, Alert],
  templateUrl: './coupon-list.html',
  styleUrl: './coupon-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CouponList implements OnInit {

  private couponService = inject(CouponService);
  private toastr        = inject(ToastrService);

  // ── State signals ──────────────────────────────────────────────
  coupons  = signal<CouponDto[]>([]);
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

    this.couponService.getAll().subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.coupons.set(res.result);
        } else {
          this.errorMsg.set(res.message || 'Failed to load coupons.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to reach the coupon service.');
      },
    });
  }
}