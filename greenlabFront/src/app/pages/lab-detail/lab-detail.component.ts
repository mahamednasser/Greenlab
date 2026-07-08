import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LabService } from '../../services/lab.service';
import { AuthService } from '../../core/auth.service';
import { Device } from '../../models/lab.model';
import { LabDetailDto, LabOverviewApiRow } from '../../models/api.types';

@Component({
  selector: 'app-lab-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './lab-detail.component.html',
  styleUrls: ['./lab-detail.component.scss'],
})
export class LabDetailComponent implements OnInit, OnDestroy {
  devices: Device[] = [];
  labName = 'Lab';
  floorLabel = '—';
  statusLabel: 'Active' | 'Idle' | 'Off' = 'Active';
  energyUsed = 0;
  energyMax = 30;
  gaugePct = 0;
  showStaffNote = false;
  occupancySummary = '';
  lastSeenSummary = '';
  loading = true;
  errorMsg: string | null = null;

  readonly gaugeTotal = 2 * Math.PI * 50;
  gaugeArc = 0;
  gaugeOffset = 0;

  private labId = NaN;
  private langSub?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly svc: LabService,
    private readonly auth: AuthService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.labId = idParam ? parseInt(idParam, 10) : NaN;
    this.load();
    this.langSub = this.translate.onLangChange.subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }

  private load(): void {
    this.loading = true;
    this.errorMsg = null;
    this.showStaffNote = false;
    this.devices = [];

    if (Number.isNaN(this.labId)) {
      this.loading = false;
      this.errorMsg = 'LAB_DETAIL.ERR_INVALID';
      return;
    }

    if (this.auth.isAdmin()) {
      forkJoin({
        lab: this.svc.getLabDetail(this.labId).pipe(catchError(() => of(null))),
        devices: this.svc.getLabDevices(this.labId).pipe(catchError(() => of([]))),
        overview: this.svc.getLabOverviewById(this.labId).pipe(catchError(() => of(null))),
      }).subscribe({
        next: ({ lab, devices, overview }) => {
          this.loading = false;
          if (!lab) {
            this.errorMsg = 'LAB_DETAIL.ERR_NOT_FOUND';
            return;
          }
          this.applyLab(lab, overview);
          this.devices = devices.map((d) => this.svc.mapDeviceDto(d));
          this.updateGauge();
        },
        error: () => {
          this.loading = false;
          this.errorMsg = 'LAB_DETAIL.ERR_LOAD';
        },
      });
      return;
    }

    if (this.auth.isStaff()) {
      this.svc.getStaffLabRow(this.labId).subscribe({
        next: (row) => {
          this.loading = false;
          if (!row) {
            this.errorMsg = 'LAB_DETAIL.ERR_STAFF';
            return;
          }
          this.labName = row.name;
          this.floorLabel = '—';
          this.statusLabel = row.inSession ? 'Active' : 'Idle';
          this.energyUsed = row.inSession ? 12 : 4;
          this.energyMax = 30;
          this.occupancySummary = row.inSession
            ? this.translate.instant('LAB_DETAIL.OCC_SESSION')
            : this.translate.instant('LAB_DETAIL.OCC_NONE');
          this.lastSeenSummary = this.translate.instant('LAB_DETAIL.STAFF_VIEW');
          this.showStaffNote = true;
          this.updateGauge();
        },
        error: () => {
          this.loading = false;
          this.errorMsg = 'LAB_DETAIL.ERR_LOAD';
        },
      });
      return;
    }

    this.loading = false;
    this.errorMsg = 'LAB_DETAIL.ERR_ROLE';
  }

  private applyLab(lab: LabDetailDto, overview: LabOverviewApiRow | null): void {
    this.labName = lab.name;
    this.floorLabel =
      lab.cvLabCode != null && lab.cvLabCode.length > 0 ? `CV: ${lab.cvLabCode}` : '—';
    this.showStaffNote = false;

    if (overview) {
      this.statusLabel = overview.inSession ? 'Active' : 'Idle';
      this.energyUsed = Math.min(
        29,
        Math.round(overview.lastOccupancyCount * 2.2 + (overview.inSession ? 7 : 2))
      );
      this.energyMax = 30;
      this.occupancySummary = this.translate.instant('LAB_DETAIL.LAST_COUNT', {
        count: overview.lastOccupancyCount,
      });
      this.lastSeenSummary = this.formatRelative(overview.lastOccupancyAt);
    } else {
      this.statusLabel = 'Active';
      this.energyUsed = 18;
      this.energyMax = 30;
      this.occupancySummary = this.translate.instant('LAB_DETAIL.NO_OVERVIEW');
      this.lastSeenSummary = '—';
    }
  }

  private formatRelative(iso: string | null): string {
    if (!iso) return this.translate.instant('LAB_DETAIL.NO_READING');
    const t = new Date(iso).getTime();
    if (Number.isNaN(t)) return '—';
    const mins = Math.round((Date.now() - t) / 60000);
    if (mins < 1) return this.translate.instant('LAB_DETAIL.UPDATED_NOW');
    if (mins < 60) return this.translate.instant('LAB_DETAIL.UPDATED_MIN', { min: mins });
    const hrs = Math.round(mins / 60);
    return this.translate.instant('LAB_DETAIL.UPDATED_HR', { hrs });
  }

  statusKey(): string {
    return `LABS.STATUS_${this.statusLabel.toUpperCase()}`;
  }

  tagKey(tag: string): string {
    return `LABS.TAG_${(tag || '').toUpperCase()}`;
  }

  runKey(d: Device): string {
    if (d.runStatus === 'NO USAGE') return 'LAB_DETAIL.RUN_NONE';
    if (d.runStatus === 'RUNNING') return 'LAB_DETAIL.RUN_RUNNING';
    return 'LAB_DETAIL.RUN_IDLE';
  }

  private updateGauge(): void {
    const pct = this.energyMax > 0 ? this.energyUsed / this.energyMax : 0;
    this.gaugePct = Math.round(pct * 100);
    const arcLen = this.gaugeTotal * pct;
    this.gaugeArc = +arcLen.toFixed(2);
    this.gaugeOffset = +(this.gaugeTotal * 0.25).toFixed(2);
  }

  iconBg(icon: string) {
    return { ac: 'bg-indigo', light: 'bg-yellow', pcs: 'bg-purple', monitor: 'bg-gray' }[icon] ?? '';
  }

  statusLblClass(s: string) {
    return { ON: 'lbl-on', IDLE: 'lbl-idle', OFF: 'lbl-off' }[s] ?? '';
  }

  runClass(s: string) {
    return { RUNNING: 'run-green', IDLE: 'run-yellow', 'NO USAGE': 'run-orange' }[s] ?? '';
  }
}
