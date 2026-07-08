import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent {
  constructor(readonly auth: AuthService) {}

  displayName(): string {
    return this.auth.user()?.fullName?.trim() || '—';
  }

  roleLabel(): string {
    const r = this.auth.user()?.role;
    if (!r) return '—';
    if (r === 'Admin') return 'Admin';
    if (r === 'Teacher') return 'Teacher';
    if (r === 'TeacherAssistant') return 'TA';
    return r;
  }

  initials(): string {
    const n = this.auth.user()?.fullName?.trim();
    if (!n) return 'GL';
    const parts = n.split(/\s+/).filter(Boolean);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  logout(): void {
    this.auth.logout();
  }
}
