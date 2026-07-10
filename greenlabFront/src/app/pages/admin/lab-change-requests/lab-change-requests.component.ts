import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { LabChangeRequestDto } from '../../../models/api.types';

@Component({
  selector: 'app-lab-change-requests',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './lab-change-requests.component.html',
  styleUrls: ['../admin.shared.scss', './lab-change-requests.component.scss'],
})
export class LabChangeRequestsComponent implements OnInit {
  rows: LabChangeRequestDto[] = [];
  loading = true;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  reviewingId: number | null = null;
  reviewNote = '';
  statusFilter = 'Pending';

  readonly statusOptions = ['Pending', 'Approved', 'Rejected', 'AutoApproved'];

  get hasPending(): boolean {
    return this.rows.some((r) => this.isPending(r));
  }

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
  }

  fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    const status = this.statusFilter || undefined;
    this.admin.getLabChangeRequests(status).subscribe({
      next: (rows) => {
        this.rows = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.LAB_CHANGE.ERROR';
      },
    });
  }

  statusKey(status: string): string {
    return `STAFF.LAB_CHANGE.STATUS.${String(status).toUpperCase()}`;
  }

  statusClass(status: string): string {
    switch (String(status).toLowerCase()) {
      case 'pending':
        return 'pending';
      case 'approved':
      case 'autoapproved':
        return 'ok';
      case 'rejected':
        return 'off';
      default:
        return 'off';
    }
  }

  formatDateTime(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toLocaleString();
  }

  isPending(row: LabChangeRequestDto): boolean {
    return String(row.status).toLowerCase() === 'pending';
  }

  review(id: number, approve: boolean): void {
    this.reviewingId = id;
    this.errorMsg = null;
    this.successMsg = null;
    this.admin
      .reviewLabChangeRequest(id, {
        approve,
        note: this.reviewNote.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.reviewingId = null;
          this.reviewNote = '';
          this.successMsg = approve
            ? 'ADMIN.LAB_CHANGE.APPROVED'
            : 'ADMIN.LAB_CHANGE.REJECTED';
          this.fetch();
        },
        error: () => {
          this.reviewingId = null;
          this.errorMsg = 'ADMIN.LAB_CHANGE.REVIEW_ERROR';
        },
      });
  }
}
