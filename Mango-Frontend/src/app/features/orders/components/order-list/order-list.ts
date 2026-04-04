import { CommonModule } from '@angular/common';
import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Spinner } from '../../../../shared/components/spinner/spinner';
import { Alert } from '../../../../shared/components/alert/alert';
import { OrderHeaderDto } from '../../../../models';
import { OrderStatus, SD } from '../../../../constants/app.constants';
import { OrderService } from '../../../../core/services/order.service';
import { AuthState } from '../../../../core/services/auth-state.service';

type FilterStatus = 'all' | 'approved' | 'readyforpickup' | 'cancelled';

@Component({
  selector: 'app-order-list',
  imports: [CommonModule, RouterLink, FormsModule, Spinner, Alert],
  templateUrl: './order-list.html',
  styleUrl: './order-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderList implements OnInit {

  private orderService = inject(OrderService);
  authState = inject(AuthState);

  // ── Constants ──────────────────────────────────────────────────
  SD = SD;

  readonly filters: { label: string; value: FilterStatus }[] = [
    { label: 'All', value: 'all' },
    { label: 'Approved', value: 'approved' },
    { label: 'Ready for pickup', value: 'readyforpickup' },
    { label: 'Cancelled', value: 'cancelled' },
  ];

  // ── State signals ──────────────────────────────────────────────
  allOrders = signal<OrderHeaderDto[]>([]);
  loading = signal(false);
  errorMsg = signal('');
  activeFilter = signal<FilterStatus>('all');

  // ── Derived signal ─────────────────────────────────────────────
  filtered = computed(() => {
    const orders = this.allOrders();
    switch (this.activeFilter()) {
      case 'approved':
        return orders.filter(o => o.status === SD.STATUS_APPROVED);
      case 'readyforpickup':
        return orders.filter(o => o.status === SD.STATUS_READY_FOR_PICKUP);
      case 'cancelled':
        return orders.filter(
          o => o.status === SD.STATUS_CANCELLED || o.status === SD.STATUS_REFUNDED
        );
      default:
        return orders;
    }
  });

  // ── Lifecycle ──────────────────────────────────────────────────
  ngOnInit(): void {
    this.load();
  }

  // ── Methods ────────────────────────────────────────────────────
  load(): void {
    this.loading.set(true);
    this.errorMsg.set('');

    const userId = this.authState.isAdmin
      ? undefined
      : this.authState.currentUser?.id;

    this.orderService.getAllOrders(userId).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.isSuccess && res.result) {
          this.allOrders.set(
            [...res.result].sort((a, b) => b.orderHeaderId - a.orderHeaderId)
          );
        } else {
          this.errorMsg.set(res.message || 'Failed to load orders.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('Unable to reach the order service.');
      },
    });
  }

  setFilter(filter: FilterStatus): void {
    this.activeFilter.set(filter);
  }

  statusBadgeClass(status: OrderStatus): string {
    const map: Record<string, string> = {
      [SD.STATUS_PENDING]: 'bg-secondary',
      [SD.STATUS_APPROVED]: 'bg-info text-dark',
      [SD.STATUS_READY_FOR_PICKUP]: 'bg-primary',
      [SD.STATUS_COMPLETED]: 'bg-success',
      [SD.STATUS_CANCELLED]: 'bg-danger',
      [SD.STATUS_REFUNDED]: 'bg-warning text-dark',
    };
    return map[status] ?? 'bg-secondary';
  }
}