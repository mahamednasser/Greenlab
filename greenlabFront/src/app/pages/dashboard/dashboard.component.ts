import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LabService } from '../../services/lab.service';
import { DashboardVm } from '../../models/dashboard.model';

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
  chartMax = 24;
  private langSub?: Subscription;

  constructor(
    private readonly svc: LabService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.load();
    this.langSub = this.translate.onLangChange.subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }

  private load(): void {
    this.loading = true;
    this.errorMsg = null;
    this.svc.getDashboardData().subscribe({
      next: (vm) => {
        this.vm = vm;
        this.loading = false;
        const maxVal = vm.labUsage.reduce((m, b) => Math.max(m, b.val), 0);
        this.chartMax = Math.max(6, maxVal, 1);
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'DASHBOARD.ERROR';
      },
    });
  }

  barHeightPct(val: number): number {
    if (!this.chartMax) return 0;
    return Math.min(100, (val / this.chartMax) * 100);
  }
}
