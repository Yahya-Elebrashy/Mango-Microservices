import { CommonModule } from '@angular/common';
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { OrderService } from '../../../../core/services/order.service';
import { OrderHeaderDto } from '../../../../models';
import { SD } from '../../../../constants/app.constants';

@Component({
  selector: 'app-confirmation',
  imports: [CommonModule, RouterLink, Spinner],
  templateUrl: './confirmation.html',
  styleUrl: './confirmation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Confirmation implements OnInit {

  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private orderService = inject(OrderService);

  // ── Constants ──────────────────────────────────────────────────
  SD = SD;

  readonly orderId = Number(this.route.snapshot.queryParamMap.get('orderId'));

  // ── State signals ──────────────────────────────────────────────
  order   = signal<OrderHeaderDto | null>(null);
  loading = signal(true);

  // ── Derived signal ─────────────────────────────────────────────
  success = computed(() => this.order()?.status === SD.STATUS_APPROVED);

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    if (!this.orderId) {
      this.router.navigate(['/']);
      return;
    }
    this.validate();
  }

  // ── Methods ────────────────────────────────────────────────────
  validate(): void {
    this.orderService.validateStripeSession(this.orderId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.order.set(res.result);
        }
      },
      error: () => this.loading.set(false),
    });
  }
}