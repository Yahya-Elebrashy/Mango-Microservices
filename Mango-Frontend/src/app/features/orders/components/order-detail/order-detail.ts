// order-detail.ts

import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { OrderStatus, SD } from '../../../../constants/app.constants';
import { OrderHeaderDto } from '../../../../models';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OrderService } from '../../../../core/services/order.service';
import { AuthState } from '../../../../core/services/auth-state.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { Alert } from '../../../../shared/components/alert/alert';
import { Spinner } from '../../../../shared/components/spinner/spinner';

@Component({
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink, Spinner, Alert],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetail implements OnInit {

  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private orderService = inject(OrderService);
  private toastr       = inject(ToastrService);
  authState            = inject(AuthState);

  SD = SD;
  readonly orderId = Number(this.route.snapshot.paramMap.get('id'));

  // ── State signals ──────────────────────────────────────────────
  order          = signal<OrderHeaderDto | null>(null);
  loading        = signal(false);
  updatingStatus = signal(false);
  errorMsg       = signal('');

  // ── Derived signals ────────────────────────────────────────────
  // ✅ Only depend on order() signal — NOT on authState.isAdmin
  // because isAdmin is a plain property, not a signal, so computed()
  // won't track it. Guard isAdmin in the template with *ngIf instead.
  canMarkReady = computed(() =>
    this.order()?.status === SD.STATUS_APPROVED
  );

  canComplete = computed(() =>
    this.order()?.status === SD.STATUS_READY_FOR_PICKUP
  );

  canCancel = computed(() => {
    const nonCancellable: OrderStatus[] = [
      SD.STATUS_COMPLETED,
      SD.STATUS_CANCELLED,
      SD.STATUS_REFUNDED,
    ];
    const current = this.order();
    return !!current && !nonCancellable.includes(current.status);
  });

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    if (!this.orderId) {
      this.router.navigate(['/order']);
      return;
    }
    this.loadOrder();
  }

  // ── Methods ────────────────────────────────────────────────────
  loadOrder(): void {
    this.loading.set(true);
    this.errorMsg.set('');

    this.orderService.getOrder(this.orderId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.order.set(res.result);
          if (
            !this.authState.isAdmin &&
            res.result.userId !== this.authState.currentUser?.id
          ) {
            this.router.navigate(['/order']);
          }
        } else {
          this.errorMsg.set(res.message || 'Order not found.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to load order.');
      },
    });
  }

  updateStatus(status: OrderStatus): void {
    this.updatingStatus.set(true);

    this.orderService.updateOrderStatus(this.orderId, status).subscribe({
      next: res => {
        this.updatingStatus.set(false);
        if (res.isSuccess) {
          this.toastr.success(`Order marked as ${status}.`);
          this.order.update(o => o ? { ...o, status } : o);
        } else {
          this.toastr.error(res.message || 'Failed to update status.');
        }
      },
      error: () => {
        this.updatingStatus.set(false);
        this.toastr.error('Failed to update status.');
      },
    });
  }

  statusBadgeClass(status: OrderStatus): string {
    const map: Record<string, string> = {
      [SD.STATUS_PENDING]:          'bg-secondary',
      [SD.STATUS_APPROVED]:         'bg-info text-dark',
      [SD.STATUS_READY_FOR_PICKUP]: 'bg-primary',
      [SD.STATUS_COMPLETED]:        'bg-success',
      [SD.STATUS_CANCELLED]:        'bg-danger',
      [SD.STATUS_REFUNDED]:         'bg-warning text-dark',
    };
    return map[status] ?? 'bg-secondary';
  }
}