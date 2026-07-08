import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LabService } from '../../services/lab.service';
import { Lab } from '../../models/lab.model';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-labs',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './labs.component.html',
  styleUrls: ['./labs.component.scss'],
})
export class LabsComponent implements OnInit, OnDestroy {
  labs: Lab[] = [];
  loading = true;
  errorMsg: string | null = null;
  private langSub?: Subscription;
  private lightBusyByLab: Record<string, boolean> = {};
  private fanBusyByLab: Record<string, boolean> = {};

  constructor(
    private readonly svc: LabService,
    private readonly translate: TranslateService,
    readonly auth: AuthService
  ) {}

  ngOnInit(): void {
    this.fetch();
    this.langSub = this.translate.onLangChange.subscribe(() => this.fetch());
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.svc.getLabs().subscribe({
      next: (labs) => {
        this.labs = labs;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'LABS.ERROR';
      },
    });
  }

  badgeClass(s: string) {
    return { Active: 'badge-active', Idle: 'badge-idle', Off: 'badge-off' }[s] ?? 'badge-off';
  }

  fillClass(lab: Lab) {
    const pct = lab.energyMax > 0 ? lab.energyUsed / lab.energyMax : 0;
    if (lab.status === 'Off') return 'fill-yellow';
    if (pct >= 0.8) return 'fill-orange';
    return 'fill-green';
  }

  tagClass(s: string) {
    return { ON: 'tag-on', IDLE: 'tag-idle', OFF: 'tag-off' }[s] ?? 'tag-off';
  }

  isLightBusy(labId: string): boolean {
    return !!this.lightBusyByLab[labId];
  }

  isFanBusy(labId: string): boolean {
    return !!this.fanBusyByLab[labId];
  }

  onToggleLight(ev: Event, lab: Lab): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (this.isLightBusy(lab.id)) return;
    const nextOn = lab.lights !== 'ON';
    const prev = lab.lights;
    lab.lights = nextOn ? 'ON' : 'OFF';
    this.lightBusyByLab[lab.id] = true;
    this.svc.setLightState(Number(lab.id), nextOn).subscribe({
      next: () => {
        this.lightBusyByLab[lab.id] = false;
      },
      error: () => {
        lab.lights = prev;
        this.lightBusyByLab[lab.id] = false;
        this.errorMsg = 'LABS.CONTROL_LIGHT_ERROR';
      },
    });
  }

  onToggleFan(ev: Event, lab: Lab): void {
    ev.preventDefault();
    ev.stopPropagation();
    if (this.isFanBusy(lab.id)) return;
    const nextOn = lab.ac !== 'ON';
    const prev = lab.ac;
    lab.ac = nextOn ? 'ON' : 'OFF';
    this.fanBusyByLab[lab.id] = true;
    this.svc.setFanState(Number(lab.id), nextOn).subscribe({
      next: () => {
        this.fanBusyByLab[lab.id] = false;
      },
      error: () => {
        lab.ac = prev;
        this.fanBusyByLab[lab.id] = false;
        this.errorMsg = 'LABS.CONTROL_FAN_ERROR';
      },
    });
  }

  statusKey(status: string): string {
    return `LABS.STATUS_${(status || '').toUpperCase()}`;
  }

  tagKey(tag: string): string {
    return `LABS.TAG_${(tag || '').toUpperCase()}`;
  }

  get activeCount(): number {
    return this.labs.filter((l) => l.status === 'Active').length;
  }
  get idleCount(): number {
    return this.labs.filter((l) => l.status === 'Idle').length;
  }
  get offCount(): number {
    return this.labs.filter((l) => l.status === 'Off').length;
  }
}
