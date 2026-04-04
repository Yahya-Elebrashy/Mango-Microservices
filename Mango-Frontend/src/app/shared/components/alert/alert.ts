import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export type AlertType = 'success' | 'danger' | 'warning' | 'info';

@Component({
  selector: 'app-alert',
  imports: [CommonModule],
  templateUrl: './alert.html',
  styleUrl: './alert.scss',
})
export class Alert {
  @Input() message = '';
  @Input() type: AlertType = 'info';
  @Output() dismissed = new EventEmitter<void>();

  get iconClass(): string {
    const icons: Record<AlertType, string> = {
      success: 'bi-check-circle-fill',
      danger: 'bi-x-circle-fill',
      warning: 'bi-exclamation-triangle-fill',
      info: 'bi-info-circle-fill',
    };
    return icons[this.type];
  }

  dismiss(): void {
    this.message = '';
    this.dismissed.emit();
  }
}
