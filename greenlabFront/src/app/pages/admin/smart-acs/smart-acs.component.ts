import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { LabDetailDto, SmartAcDto } from '../../../models/api.types';

@Component({
  selector: 'app-smart-acs',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './smart-acs.component.html',
  styleUrls: ['../admin.shared.scss', './smart-acs.component.scss'],
})
export class SmartAcsComponent implements OnInit {
  rows: SmartAcDto[] = [];
  labs: LabDetailDto[] = [];
  loading = true;
  saving = false;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  showForm = false;
  editingId: number | null = null;

  labId = 0;
  name = '';
  ipAddress = '';
  port = 80;
  pathPrefix = '';
  isActive = true;

  busyPower: Record<number, boolean> = {};

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.listLabs().subscribe({
      next: (labs) => {
        this.labs = labs;
        if (!this.labId && labs.length) this.labId = labs[0].id;
      },
    });
    this.admin.listSmartAcs().subscribe({
      next: (rows) => {
        this.rows = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.SMART_ACS.ERROR';
      },
    });
  }

  openCreate(): void {
    this.showForm = true;
    this.editingId = null;
    this.errorMsg = null;
    this.successMsg = null;
    this.name = '';
    this.ipAddress = '';
    this.port = 80;
    this.pathPrefix = '';
    this.isActive = true;
    if (this.labs.length) this.labId = this.labs[0].id;
  }

  openEdit(row: SmartAcDto): void {
    this.showForm = true;
    this.editingId = row.id;
    this.errorMsg = null;
    this.successMsg = null;
    this.labId = row.labId;
    this.name = row.name;
    this.ipAddress = row.ipAddress;
    this.port = row.port || 80;
    this.pathPrefix = row.pathPrefix ?? '';
    this.isActive = row.isActive;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  submit(): void {
    if (!this.labId || !this.name.trim() || !this.ipAddress.trim()) {
      this.errorMsg = 'ADMIN.SMART_ACS.VALIDATION';
      return;
    }
    this.saving = true;
    this.errorMsg = null;
    this.successMsg = null;

    const body = {
      labId: Number(this.labId),
      name: this.name.trim(),
      ipAddress: this.ipAddress.trim(),
      port: Number(this.port) || 80,
      pathPrefix: this.pathPrefix.trim() || null,
      isActive: this.isActive,
    };

    const req$ =
      this.editingId == null
        ? this.admin.createSmartAc({
            labId: body.labId,
            name: body.name,
            ipAddress: body.ipAddress,
            port: body.port,
            pathPrefix: body.pathPrefix,
          })
        : this.admin.updateSmartAc(this.editingId, body);

    req$.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.successMsg = 'ADMIN.SMART_ACS.SAVED';
        this.fetch();
      },
      error: (err) => {
        this.saving = false;
        this.errorMsg = err?.error?.title || err?.error?.detail || 'ADMIN.SMART_ACS.SAVE_ERROR';
      },
    });
  }

  remove(row: SmartAcDto): void {
    if (!confirm(`Delete ${row.name}?`)) return;
    this.admin.deleteSmartAc(row.id).subscribe({
      next: () => this.fetch(),
      error: () => (this.errorMsg = 'ADMIN.SMART_ACS.DELETE_ERROR'),
    });
  }

  togglePower(row: SmartAcDto): void {
    if (this.busyPower[row.id]) return;
    const next = !(row.lastPowerOn === true);
    this.busyPower[row.id] = true;
    this.admin.setSmartAcPower(row.id, next).subscribe({
      next: (updated) => {
        Object.assign(row, updated);
        this.busyPower[row.id] = false;
      },
      error: () => {
        this.busyPower[row.id] = false;
        this.errorMsg = 'ADMIN.SMART_ACS.CONTROL_ERROR';
      },
    });
  }

  get isEditing(): boolean {
    return this.editingId != null;
  }
}
