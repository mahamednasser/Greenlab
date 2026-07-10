import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LabService } from '../../services/lab.service';
import { DashboardVm } from '../../models/dashboard.model';
import { formatRuntime } from '../../utils/power';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, OnDestroy {
  vm: DashboardVm | null = null;
  loading = true;
  errorMsg: string | null = null;
  chartMax = 0.1;
  weekPoints: { x: number; y: number }[] = [];
  weekLinePath = '';
  weekAreaPath = '';
  todayIdx = (new Date().getDay() + 6) % 7;
  private langSub?: Subscription;
  private refreshTimer?: ReturnType<typeof setInterval>;

  constructor(
    private readonly svc: LabService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.load();
    this.langSub = this.translate.onLangChange.subscribe(() => this.load());
    // Refresh so runtime/kWh keep accumulating while loads stay ON.
    this.refreshTimer = setInterval(() => this.load(true), 15000);
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  get lightRuntimeLabel(): string {
    return formatRuntime(this.vm?.lightRuntimeSeconds ?? 0);
  }

  get fanRuntimeLabel(): string {
    return formatRuntime(this.vm?.fanRuntimeSeconds ?? 0);
  }

  private load(silent = false): void {
    if (!silent) {
      this.loading = true;
      this.errorMsg = null;
    }
    this.svc.getDashboardData().subscribe({
      next: (vm) => {
        this.vm = vm;
        this.loading = false;
        const maxVal = vm.labUsage.reduce((m, b) => Math.max(m, b.val), 0);
        this.chartMax = Math.max(0.01, maxVal * 1.15, vm.displayKwhTotal * 1.15);
        this.buildWeekPaths(vm.weekData);
      },
      error: () => {
        if (!silent) {
          this.loading = false;
          this.errorMsg = 'DASHBOARD.ERROR';
        }
      },
    });
  }

  barHeightPct(val: number): number {
    if (!this.chartMax) return 0;
    return Math.min(100, (val / this.chartMax) * 100);
  }

  private buildWeekPaths(series: { day: string; val: number }[]): void {
    if (!series.length) {
      this.weekPoints = [];
      this.weekLinePath = '';
      this.weekAreaPath = '';
      return;
    }
    const max = Math.max(...series.map((s) => s.val), 0.01);
    const n = series.length;
    const pts = series.map((s, i) => {
      const x = n === 1 ? 280 : (i / (n - 1)) * 560;
      const y = 158 - (s.val / max) * 120;
      return { x, y };
    });
    this.weekPoints = pts;
    this.weekLinePath = pts
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`)
      .join(' ');
    const last = pts[pts.length - 1];
    const first = pts[0];
    this.weekAreaPath = `${this.weekLinePath} L${last.x.toFixed(1)},190 L${first.x.toFixed(1)},190 Z`;
  }
}
