import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LabService } from '../../services/lab.service';
import { AuthService } from '../../core/auth.service';
import { Device } from '../../models/lab.model';
import { LabDetailDto, LabOverviewApiRow, SmartAcDto, readLabOverride } from '../../models/api.types';
import { LAB_MAX_WATTS, LIGHT_WATTS, FAN_WATTS, energyFromRuntime, labWatts } from '../../utils/power';

@Component({
  selector: 'app-lab-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './lab-detail.component.html',
  styleUrls: ['./lab-detail.component.scss'],
})
export class LabDetailComponent implements OnInit, OnDestroy {
  devices: Device[] = [];
  smartAcs: SmartAcDto[] = [];
  labName = 'Lab';
  floorLabel = '—';
  statusLabel: 'Active' | 'Idle' | 'Off' = 'Active';
  /** Accumulated energy (kWh) shown on the gauge. */
  energyUsed = 0;
  energyMax = LAB_MAX_WATTS;
  currentWatts = 0;
  gaugePct = 0;
  showStaffNote = false;
  occupancySummary = '';
  lastSeenSummary = '';
  loading = true;
  errorMsg: string | null = null;
  controlError: string | null = null;

  lightOn = false;
  fanOn = false;
  /** false = sensor active (default); true = locked. */
  motionSensorDisabled = false;
  lightBusy = false;
  fanBusy = false;
  motionBusy = false;
  acBusy: Record<number, boolean> = {};
  acTempBusy: Record<number, boolean> = {};
  peopleCount = 0;

  readonly lightWatts = LIGHT_WATTS;
  readonly fanWatts = FAN_WATTS;
  readonly gaugeTotal = 2 * Math.PI * 50;
  gaugeArc = 0;
  gaugeOffset = 0;

  labId = NaN;
  private langSub?: Subscription;
  private refreshTimer?: ReturnType<typeof setInterval>;

  get isAdmin(): boolean {
    return this.auth.isAdmin();
  }

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
    // Match backend occupancy poll so people count stays live on this page.
    this.refreshTimer = setInterval(() => this.load(true), 15000);
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  private load(silent = false): void {
    if (!silent) {
      this.loading = true;
      this.errorMsg = null;
      this.controlError = null;
      this.showStaffNote = false;
      this.devices = [];
      this.smartAcs = [];
    }

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
        smartAcs: this.svc.getSmartAcsByLab(this.labId).pipe(catchError(() => of([]))),
      }).subscribe({
        next: ({ lab, devices, overview, smartAcs }) => {
          this.loading = false;
          if (!lab) {
            if (!silent) this.errorMsg = 'LAB_DETAIL.ERR_NOT_FOUND';
            return;
          }
          this.applyLab(lab, overview);
          this.smartAcs = smartAcs;
          if (!silent) {
            this.devices = devices.map((d) => this.svc.mapDeviceDto(d));
          }
          this.updateGauge();
        },
        error: () => {
          if (!silent) {
            this.loading = false;
            this.errorMsg = 'LAB_DETAIL.ERR_LOAD';
          }
        },
      });
      return;
    }

    if (this.auth.isStaff()) {
      forkJoin({
        row: this.svc.getStaffLabRow(this.labId).pipe(catchError(() => of(null))),
        smartAcs: this.svc.getSmartAcsByLab(this.labId).pipe(catchError(() => of([]))),
      }).subscribe({
        next: ({ row, smartAcs }) => {
          this.loading = false;
          if (!row) {
            if (!silent) this.errorMsg = 'LAB_DETAIL.ERR_STAFF';
            return;
          }
          this.labName = row.name;
          this.floorLabel = '—';
          this.lightOn = !!row.actualLightOn;
          this.fanOn = !!row.actualFanOn;
          this.motionSensorDisabled = !!readLabOverride(row)?.motionSensorDisabled;
          this.peopleCount = Number(row.lastOccupancyCount ?? 0);
          this.statusLabel = row.inSession
            ? 'Active'
            : this.lightOn || this.fanOn
              ? 'Idle'
              : 'Off';
          const lightSec = Number(row.lightRuntimeSeconds ?? 0);
          const fanSec = Number(row.fanRuntimeSeconds ?? 0);
          this.energyUsed = Number(row.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
          this.currentWatts = Number(row.currentWatts ?? 0) || labWatts(this.lightOn, this.fanOn);
          this.energyMax = LAB_MAX_WATTS;
          this.occupancySummary = this.translate.instant('LAB_DETAIL.LAST_COUNT', {
            count: this.peopleCount,
          });
          this.lastSeenSummary = row.lastOccupancyAt
            ? this.formatRelative(row.lastOccupancyAt)
            : this.translate.instant('LAB_DETAIL.STAFF_VIEW');
          this.showStaffNote = true;
          this.smartAcs = smartAcs;
          this.updateGauge();
        },
        error: () => {
          if (!silent) {
            this.loading = false;
            this.errorMsg = 'LAB_DETAIL.ERR_LOAD';
          }
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
      this.lightOn = !!overview.actualLightOn;
      this.fanOn = !!overview.actualFanOn;
      this.motionSensorDisabled = !!readLabOverride(overview)?.motionSensorDisabled;
      this.peopleCount = overview.lastOccupancyCount ?? 0;
      this.statusLabel = overview.inSession
        ? 'Active'
        : this.lightOn || this.fanOn
          ? 'Idle'
          : 'Off';
      const lightSec = Number(overview.lightRuntimeSeconds ?? 0);
      const fanSec = Number(overview.fanRuntimeSeconds ?? 0);
      this.energyUsed = Number(overview.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
      this.currentWatts =
        Number(overview.currentWatts ?? 0) || labWatts(this.lightOn, this.fanOn);
      this.energyMax = LAB_MAX_WATTS;
      this.occupancySummary = this.translate.instant('LAB_DETAIL.LAST_COUNT', {
        count: this.peopleCount,
      });
      this.lastSeenSummary = this.formatRelative(overview.lastOccupancyAt);
    } else {
      this.statusLabel = 'Off';
      this.lightOn = false;
      this.fanOn = false;
      this.motionSensorDisabled = false;
      this.peopleCount = 0;
      this.energyUsed = 0;
      this.currentWatts = 0;
      this.energyMax = LAB_MAX_WATTS;
      this.occupancySummary = this.translate.instant('LAB_DETAIL.NO_OVERVIEW');
      this.lastSeenSummary = '—';
    }
  }

  toggleLight(): void {
    if (this.lightBusy || Number.isNaN(this.labId)) return;
    const next = !this.lightOn;
    const prev = this.lightOn;
    this.lightOn = next;
    this.lightBusy = true;
    this.controlError = null;
    this.syncPowerFromLoads();
    this.svc.setLightState(this.labId, next).subscribe({
      next: () => {
        this.lightBusy = false;
      },
      error: () => {
        this.lightOn = prev;
        this.lightBusy = false;
        this.syncPowerFromLoads();
        this.controlError = 'LABS.CONTROL_LIGHT_ERROR';
      },
    });
  }

  toggleFan(): void {
    if (this.fanBusy || Number.isNaN(this.labId)) return;
    const next = !this.fanOn;
    const prev = this.fanOn;
    this.fanOn = next;
    this.fanBusy = true;
    this.controlError = null;
    this.syncPowerFromLoads();
    this.svc.setFanState(this.labId, next).subscribe({
      next: () => {
        this.fanBusy = false;
      },
      error: () => {
        this.fanOn = prev;
        this.fanBusy = false;
        this.syncPowerFromLoads();
        this.controlError = 'LABS.CONTROL_FAN_ERROR';
      },
    });
  }

  toggleMotionLock(): void {
    if (this.motionBusy || Number.isNaN(this.labId)) return;
    const nextDisabled = !this.motionSensorDisabled;
    const prev = this.motionSensorDisabled;
    this.motionSensorDisabled = nextDisabled;
    this.motionBusy = true;
    this.controlError = null;
    this.svc.setMotionSensorDisabled(this.labId, nextDisabled).subscribe({
      next: () => {
        this.motionBusy = false;
      },
      error: () => {
        this.motionSensorDisabled = prev;
        this.motionBusy = false;
        this.controlError = 'LABS.CONTROL_MOTION_ERROR';
      },
    });
  }

  toggleSmartAc(ac: SmartAcDto): void {
    if (this.acBusy[ac.id]) return;
    const next = !(ac.lastPowerOn === true);
    const prev = ac.lastPowerOn;
    ac.lastPowerOn = next;
    this.acBusy[ac.id] = true;
    this.controlError = null;
    this.svc.setSmartAcPower(ac.id, next).subscribe({
      next: (updated) => {
        Object.assign(ac, updated);
        this.acBusy[ac.id] = false;
      },
      error: () => {
        ac.lastPowerOn = prev;
        this.acBusy[ac.id] = false;
        this.controlError = 'LAB_DETAIL.AC_POWER_ERROR';
      },
    });
  }

  setSmartAcTemp(ac: SmartAcDto, celsius: number): void {
    if (this.acTempBusy[ac.id]) return;
    const temp = Math.min(30, Math.max(16, Math.round(celsius)));
    const prev = ac.lastTargetTempC;
    ac.lastTargetTempC = temp;
    this.acTempBusy[ac.id] = true;
    this.controlError = null;
    this.svc.setSmartAcTemp(ac.id, temp).subscribe({
      next: (updated) => {
        Object.assign(ac, updated);
        this.acTempBusy[ac.id] = false;
      },
      error: () => {
        ac.lastTargetTempC = prev;
        this.acTempBusy[ac.id] = false;
        this.controlError = 'LAB_DETAIL.AC_TEMP_ERROR';
      },
    });
  }

  bumpAcTemp(ac: SmartAcDto, delta: number): void {
    const current = ac.lastTargetTempC ?? 24;
    this.setSmartAcTemp(ac, current + delta);
  }

  private syncPowerFromLoads(): void {
    this.currentWatts = labWatts(this.lightOn, this.fanOn);
    if (this.statusLabel !== 'Active') {
      this.statusLabel = this.lightOn || this.fanOn ? 'Idle' : 'Off';
    }
    this.updateGauge();
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
    const pct = this.energyMax > 0 ? this.currentWatts / this.energyMax : 0;
    this.gaugePct = Math.round(Math.min(1, pct) * 100);
    const arcLen = this.gaugeTotal * Math.min(1, pct);
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
