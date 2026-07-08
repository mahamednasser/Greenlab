import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { UserListItem } from '../../../models/api.types';
import { HttpErrorResponse } from '@angular/common/http';

const ROLES = ['Admin', 'Teacher', 'TeacherAssistant'] as const;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './users.component.html',
  styleUrls: ['../admin.shared.scss', './users.component.scss'],
})
export class UsersComponent implements OnInit {
  users: UserListItem[] = [];
  loading = true;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  saving = false;

  showForm = false;
  editingId: number | null = null;

  fullName = '';
  email = '';
  role: (typeof ROLES)[number] = 'Teacher';
  password = '';
  isActive = true;

  readonly roleOptions = ROLES;

  constructor(
    private readonly admin: AdminService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.fetch();
  }

  get isEditing(): boolean {
    return this.editingId != null;
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.getUsers().subscribe({
      next: (rows) => {
        this.users = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.USERS.ERROR';
      },
    });
  }

  roleKey(role: string): string {
    return `ADMIN.ROLES.${role.toUpperCase()}`;
  }

  openCreate(): void {
    this.editingId = null;
    this.fullName = '';
    this.email = '';
    this.role = 'Teacher';
    this.password = '';
    this.isActive = true;
    this.showForm = true;
    this.errorMsg = null;
    this.successMsg = null;
  }

  openEdit(u: UserListItem): void {
    this.editingId = u.id;
    this.fullName = u.fullName;
    this.email = u.email;
    this.role = (ROLES.includes(u.role as (typeof ROLES)[number])
      ? u.role
      : 'Teacher') as (typeof ROLES)[number];
    this.password = '';
    this.isActive = u.isActive;
    this.showForm = true;
    this.errorMsg = null;
    this.successMsg = null;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.password = '';
  }

  submit(): void {
    if (!this.fullName.trim() || !this.email.trim()) return;
    if (!this.isEditing && this.password.length < 6) {
      this.errorMsg = 'ADMIN.USERS.PASSWORD_MIN';
      return;
    }
    if (this.isEditing && this.password && this.password.length < 6) {
      this.errorMsg = 'ADMIN.USERS.PASSWORD_MIN';
      return;
    }

    this.saving = true;
    this.errorMsg = null;
    this.successMsg = null;

    if (this.isEditing) {
      const body = {
        email: this.email.trim(),
        fullName: this.fullName.trim(),
        role: this.role,
        isActive: this.isActive,
        ...(this.password.trim() ? { password: this.password } : {}),
      };
      this.admin.updateUser(this.editingId!, body).subscribe({
        next: () => this.onSaveSuccess('ADMIN.USERS.UPDATED'),
        error: (err) => this.onSaveError(err),
      });
    } else {
      this.admin
        .createUser({
          email: this.email.trim(),
          fullName: this.fullName.trim(),
          password: this.password,
          role: this.role,
          isActive: this.isActive,
        })
        .subscribe({
          next: () => this.onSaveSuccess('ADMIN.USERS.CREATED'),
          error: (err) => this.onSaveError(err),
        });
    }
  }

  private onSaveSuccess(msgKey: string): void {
    this.saving = false;
    this.successMsg = msgKey;
    this.showForm = false;
    this.editingId = null;
    this.password = '';
    this.fetch();
  }

  private onSaveError(err: unknown): void {
    this.saving = false;
    const apiMsg = this.readApiError(err);
    this.errorMsg = apiMsg ?? 'ADMIN.USERS.SAVE_ERROR';
  }

  private readApiError(err: unknown): string | null {
    if (!(err instanceof HttpErrorResponse)) return null;
    const body = err.error;
    if (typeof body === 'string' && body.trim()) return body;
    if (body && typeof body === 'object') {
      const o = body as Record<string, unknown>;
      if (typeof o['title'] === 'string') return o['title'];
      if (typeof o['detail'] === 'string') return o['detail'];
    }
    return null;
  }

  deleteUser(u: UserListItem): void {
    const msg = this.translate.instant('ADMIN.USERS.DELETE_CONFIRM', {
      name: u.fullName,
      email: u.email,
    });
    if (!confirm(msg)) return;
    this.errorMsg = null;
    this.successMsg = null;
    this.admin.deleteUser(u.id).subscribe({
      next: () => {
        this.successMsg = 'ADMIN.USERS.DELETED';
        this.fetch();
      },
      error: (err) => {
        const apiMsg = this.readApiError(err);
        this.errorMsg = apiMsg ?? 'ADMIN.USERS.DELETE_ERROR';
      },
    });
  }
}
