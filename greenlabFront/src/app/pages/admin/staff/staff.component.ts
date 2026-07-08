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

  showForm = false;
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

  dayLabel(day: number | null): string {
    if (day == null) return 'MONDAY';
    return DAY_KEYS[day] ?? 'MONDAY';
  }

  formatTime(value: string | null): string {
    if (!value) return '';
    return value.length >= 5 ? value.slice(0, 5) : value;
  }

  isWeekly(row: StaffAssignmentRow): boolean {
    return row.dayOfWeek != null && !!row.startTime && !!row.endTime;
  }

  isDateRange(row: StaffAssignmentRow): boolean {
    return !!row.validFrom && !!row.validUntil;
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso.slice(0, 10);
    return d.toLocaleDateString();
  }

  sourceKey(source: string): string {
    return `ADMIN.STAFF.SOURCES.${source.toUpperCase()}`;
  }

  roleKey(role: string): string {
    return `ADMIN.ROLES.${role.toUpperCase()}`;
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    this.successMsg = null;
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

    this.admin.createStaffAssignment(body).subscribe({
      next: () => {
        this.saving = false;
        this.successMsg = 'ADMIN.STAFF.SAVED';
        this.showForm = false;
        this.fetch();
      },
      error: () => {
        this.saving = false;
        this.errorMsg = 'ADMIN.STAFF.SAVE_ERROR';
      },
    });
  }
}
