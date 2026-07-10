import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { StaffService } from '../../../services/staff.service';
import {
  CreateLabChangeRequestDto,
  CurrentLabApiRow,
  LabChangeRequestDto,
  LabOptionDto,
} from '../../../models/api.types';

@Component({
  selector: 'app-lab-change',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './lab-change.component.html',
  styleUrls: ['../../admin/admin.shared.scss', './lab-change.component.scss'],
})
export class LabChangeComponent implements OnInit {
  rows: LabChangeRequestDto[] = [];
  myLabs: CurrentLabApiRow[] = [];
  allLabs: LabOptionDto[] = [];
  loading = true;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  saving = false;
  showForm = false;

  fromLabId = 0;
  toLabId = 0;
  sectionStart = '';
  sectionEnd = '';

  constructor(private readonly staff: StaffService) {}

  ngOnInit(): void {
    this.fetch();
    this.staff.getCurrentLabs().subscribe({
      next: (labs) => {
        this.myLabs = labs;
        if (labs.length && !this.fromLabId) this.fromLabId = labs[0].id;
      },
    });
    this.staff.listLabs().subscribe({
      next: (labs) => {
        this.allLabs = labs;
        if (labs.length && !this.toLabId) this.toLabId = labs[0].id;
      },
    });
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.staff.getMyLabChangeRequests().subscribe({
      next: (rows) => {
        this.rows = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'STAFF.LAB_CHANGE.ERROR';
      },
    });
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    this.successMsg = null;
    this.errorMsg = null;
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

  submit(): void {
    if (!this.fromLabId || !this.toLabId || !this.sectionStart || !this.sectionEnd) {
      this.errorMsg = 'STAFF.LAB_CHANGE.REQUIRED';
      return;
    }
    if (this.fromLabId === this.toLabId) {
      this.errorMsg = 'STAFF.LAB_CHANGE.SAME_LAB';
      return;
    }

    const start = new Date(this.sectionStart);
    const end = new Date(this.sectionEnd);
    if (!(start < end)) {
      this.errorMsg = 'STAFF.LAB_CHANGE.INVALID_RANGE';
      return;
    }

    this.saving = true;
    this.errorMsg = null;
    this.successMsg = null;

    const body: CreateLabChangeRequestDto = {
      fromLabId: this.fromLabId,
      toLabId: this.toLabId,
      sectionStart: start.toISOString(),
      sectionEnd: end.toISOString(),
    };

    this.staff.createLabChangeRequest(body).subscribe({
      next: () => {
        this.saving = false;
        this.successMsg = 'STAFF.LAB_CHANGE.SAVED';
        this.showForm = false;
        this.fetch();
      },
      error: (err) => {
        this.saving = false;
        const msg = err?.error?.message || err?.error?.title;
        this.errorMsg = typeof msg === 'string' && msg.trim()
          ? msg
          : 'STAFF.LAB_CHANGE.SAVE_ERROR';
      },
    });
  }
}
