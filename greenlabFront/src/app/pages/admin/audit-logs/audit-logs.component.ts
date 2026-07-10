import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { AuditLogDto } from '../../../models/api.types';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['../admin.shared.scss', './audit-logs.component.scss'],
})
export class AuditLogsComponent implements OnInit {
  logs: AuditLogDto[] = [];
  filteredLogs: AuditLogDto[] = [];
  loading = true;
  errorMsg: string | null = null;

  // Filters
  searchTerm = '';
  selectedAction = '';
  selectedLabId: number | null = null;

  // Expandable details tracking
  expandedLogId: number | null = null;

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
  }

  fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.getAuditLogs().subscribe({
      next: (data) => {
        this.logs = data;
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.AUDIT_LOGS.ERROR';
      },
    });
  }

  // Get unique actions for filtering dropdown
  get uniqueActions(): string[] {
    return Array.from(new Set(this.logs.map((l) => l.action))).sort();
  }

  // Get unique labs for filtering dropdown
  get uniqueLabs(): { id: number; name: string }[] {
    const map = new Map<number, string>();
    this.logs.forEach((l) => {
      if (l.labId && l.labName) {
        map.set(l.labId, l.labName);
      }
    });
    return Array.from(map.entries()).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name));
  }

  applyFilters(): void {
    this.filteredLogs = this.logs.filter((log) => {
      // Search term (User name, email, action, details)
      const matchesSearch =
        !this.searchTerm.trim() ||
        (log.userFullName || '').toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (log.userEmail || '').toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (log.action || '').toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (log.details || '').toLowerCase().includes(this.searchTerm.toLowerCase());

      // Action Filter
      const matchesAction = !this.selectedAction || log.action === this.selectedAction;

      // Lab Filter
      const matchesLab = this.selectedLabId === null || log.labId === this.selectedLabId;

      return matchesSearch && matchesAction && matchesLab;
    });
  }

  toggleExpand(logId: number): void {
    if (this.expandedLogId === logId) {
      this.expandedLogId = null;
    } else {
      this.expandedLogId = logId;
    }
  }

  // Helper to format action names into nice badge classes
  getActionBadgeClass(action: string): string {
    switch (action.toLowerCase()) {
      case 'login':
      case 'register':
        return 'badge-info';
      case 'adminoverride':
      case 'manualoverride':
        return 'badge-warning';
      case 'labchangerequest':
      case 'scheduleimport':
        return 'badge-success';
      default:
        return 'badge-secondary';
    }
  }

  // Helper to parse details if it's JSON, and pretty-print it
  formatDetails(details: string | null): string {
    if (!details) return '';
    try {
      const parsed = JSON.parse(details);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return details;
    }
  }
}
