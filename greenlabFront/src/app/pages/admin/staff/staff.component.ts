import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import {
  CreateStaffAssignmentRequest,
  LabDetailDto,
  StaffAssignmentRow,
  UserListItem,
} from '../../../models/api.types';

const DAY_KEYS = ['SUNDAY', 'MONDAY', 'TUESDAY', 'WEDNESDAY', 'THURSDAY', 'FRIDAY', 'SATURDAY'];

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './staff.component.html',
  styleUrls: ['../admin.shared.scss', './staff.component.scss'],
})
export class StaffComponent implements OnInit {
  rows: StaffAssignmentRow[] = [];
  labs: LabDetailDto[] = [];
  staffUsers: UserListItem[] = [];
  loading = true;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  saving = false;
  deletingId: number | null = null;

  showForm = false;
  editingId: number | null = null;
  staffEmail = '';
  labId = 0;
  dayOfWeek = 1;
  startTime = '09:00';
  endTime = '17:00';
  useDateRange = false;
  validFrom = '';
  validUntil = '';

  readonly dayOptions = DAY_KEYS.map((key, i) => ({ value: i, key }));

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
    this.admin.listLabs().subscribe({
      next: (labs) => {
        this.labs = labs;
        if (labs.length && !this.labId) this.labId = labs[0].id;
      },
    });
    this.admin.getUsers().subscribe({
      next: (users) => {
        this.staffUsers = users.filter(
          (u) => u.role === 'Teacher' || u.role === 'TeacherAssistant'
        );
        if (this.staffUsers.length && !this.staffEmail) {
          this.staffEmail = this.staffUsers[0].email;
        }
      },
    });
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.getStaffAssignments().subscribe({
      next: (rows) => {
        this.rows = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.STAFF.ERROR';
      },
    });
  }

  /** Normalize API day (number or enum name) to 0–6. */
  private normalizeDay(day: number | string | null | undefined): number | null {
    if (day == null || day === '') return null;
    if (typeof day === 'number' && Number.isFinite(day)) {
      return day >= 0 && day <= 6 ? day : null;
    }
    const asNum = Number(day);
    if (Number.isFinite(asNum) && asNum >= 0 && asNum <= 6) return asNum;
    const key = String(day).trim().toUpperCase();
    const idx = DAY_KEYS.indexOf(key);
    return idx >= 0 ? idx : null;
  }

  dayLabel(day: number | string | null): string {
    const n = this.normalizeDay(day);
    if (n == null) return 'MONDAY';
    return DAY_KEYS[n] ?? 'MONDAY';
  }

  formatTime(value: string | null): string {
    if (!value) return '';
    return value.length >= 5 ? value.slice(0, 5) : value;
  }

  isWeekly(row: StaffAssignmentRow): boolean {
    return this.normalizeDay(row.dayOfWeek) != null && !!row.startTime && !!row.endTime;
  }

  isDateRange(row: StaffAssignmentRow): boolean {
    return !!row.validFrom && !!row.validUntil;
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso.slice(0, 10);
    return d.toLocaleDateString();
  }

  private toLocalInput(iso: string | null): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  sourceKey(source: string): string {
    return `ADMIN.STAFF.SOURCES.${source.toUpperCase()}`;
  }

  roleKey(role: string): string {
    return `ADMIN.ROLES.${role.toUpperCase()}`;
  }

  get isEditing(): boolean {
    return this.editingId != null;
  }

  private resetFormFields(): void {
    this.editingId = null;
    this.dayOfWeek = 1;
    this.startTime = '09:00';
    this.endTime = '17:00';
    this.useDateRange = false;
    this.validFrom = '';
    this.validUntil = '';
    if (this.staffUsers.length) this.staffEmail = this.staffUsers[0].email;
    if (this.labs.length) this.labId = this.labs[0].id;
  }

  toggleForm(): void {
    if (this.showForm) {
      this.showForm = false;
      this.resetFormFields();
    } else {
      this.resetFormFields();
      this.showForm = true;
    }
    this.successMsg = null;
    this.errorMsg = null;
  }

  cancelForm(): void {
    this.showForm = false;
    this.resetFormFields();
    this.errorMsg = null;
  }

  startEdit(row: StaffAssignmentRow): void {
    this.editingId = row.id;
    this.staffEmail = row.staffEmail;
    this.labId = row.labId;
    this.successMsg = null;
    this.errorMsg = null;

    if (this.isWeekly(row)) {
      this.useDateRange = false;
      this.dayOfWeek = this.normalizeDay(row.dayOfWeek) ?? 1;
      this.startTime = this.formatTime(row.startTime) || '09:00';
      this.endTime = this.formatTime(row.endTime) || '17:00';
      this.validFrom = '';
      this.validUntil = '';
    } else {
      this.useDateRange = true;
      this.validFrom = this.toLocalInput(row.validFrom);
      this.validUntil = this.toLocalInput(row.validUntil);
    }
    this.showForm = true;
  }

  deleteRow(row: StaffAssignmentRow): void {
    if (!confirm(`Delete schedule for ${row.staffName} / ${row.labName}?`)) return;
    this.deletingId = row.id;
    this.errorMsg = null;
    this.successMsg = null;
    this.admin.deleteStaffAssignment(row.id).subscribe({
      next: () => {
        this.deletingId = null;
        this.successMsg = 'ADMIN.STAFF.DELETED';
        if (this.editingId === row.id) this.cancelForm();
        this.fetch();
      },
      error: () => {
        this.deletingId = null;
        this.errorMsg = 'ADMIN.STAFF.DELETE_ERROR';
      },
    });
  }

  submit(): void {
    if (!this.staffEmail.trim() || !this.labId) return;
    this.saving = true;
    this.errorMsg = null;
    this.successMsg = null;

    const body: CreateStaffAssignmentRequest = {
      staffEmail: this.staffEmail.trim(),
      labId: this.labId,
    };

    if (this.useDateRange) {
      if (!this.validFrom || !this.validUntil) {
        this.saving = false;
        this.errorMsg = 'ADMIN.STAFF.RANGE_REQUIRED';
        return;
      }
      body.validFrom = new Date(this.validFrom).toISOString();
      body.validUntil = new Date(this.validUntil).toISOString();
    } else {
      body.dayOfWeek = this.dayOfWeek;
      body.startTime = `${this.startTime}:00`;
      body.endTime = `${this.endTime}:00`;
    }

    const req$ = this.editingId != null
      ? this.admin.updateStaffAssignment(this.editingId, body)
      : this.admin.createStaffAssignment(body);

    req$.subscribe({
      next: () => {
        this.saving = false;
        this.successMsg = this.editingId != null ? 'ADMIN.STAFF.UPDATED' : 'ADMIN.STAFF.SAVED';
        this.showForm = false;
        this.resetFormFields();
        this.fetch();
      },
      error: () => {
        this.saving = false;
        this.errorMsg = 'ADMIN.STAFF.SAVE_ERROR';
      },
    });
  }
}
