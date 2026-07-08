import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { Lab, Device, Alert, Activity, LabStatus } from '../models/lab.model';
import { DashboardVm } from '../models/dashboard.model';
import {
  CurrentLabApiRow,
  LabDetailDto,
  LabDeviceDto,
  LabOverviewApiRow,
  readLabOverride,
} from '../models/api.types';
import { AuthService } from '../core/auth.service';

@Injectable({ providedIn: 'root' })
export class LabService {
  private readonly api = '/api';

  constructor(
    private readonly http: HttpClient,
    private readonly auth: AuthService
  ) {}

  /** Labs list: admin uses overview (session + occupancy); staff uses current-labs. */
  getLabs(): Observable<Lab[]> {
    if (this.auth.isAdmin()) {
      return this.getAdminLabsOverview().pipe(map((rows) => rows.map((r) => this.mapOverviewToLab(r))));
    }
    if (this.auth.isStaff()) {
      return this.http.get<CurrentLabApiRow[]>(`${this.api}/staff/me/current-labs`).pipe(
        map((rows) => rows.map((r) => this.mapStaffLab(r)))
      );
    }
    return of([]);
  }

  getAdminLabsOverview(): Observable<LabOverviewApiRow[]> {
    return this.http.get<LabOverviewApiRow[]>(`${this.api}/admin/labs/overview`);
  }

  getLabOverviewById(id: number): Observable<LabOverviewApiRow | null> {
    return this.getAdminLabsOverview().pipe(
      map((rows) => rows.find((r) => r.id === id) ?? null)
    );
  }

  getLabDetail(id: number): Observable<LabDetailDto | null> {
    if (!this.auth.isAdmin()) {
      return of(null);
    }
    return this.http.get<LabDetailDto>(`${this.api}/admin/labs/${id}`);
  }

  getLabDevices(labId: number): Observable<LabDeviceDto[]> {
    if (!this.auth.isAdmin()) {
      return of([]);
    }
    return this.http.get<LabDeviceDto[]>(`${this.api}/admin/labs/${labId}/devices`);
  }

  getStaffLabRow(labId: number): Observable<CurrentLabApiRow | null> {
    return this.http.get<CurrentLabApiRow[]>(`${this.api}/staff/me/current-labs`).pipe(
      map((rows) => rows.find((r) => r.id === labId) ?? null)
    );
  }

  setLightState(labId: number, on: boolean, deviceId = 1): Observable<void> {
    const action = on ? 'on' : 'off';
    return this.http
      .post(`${this.api}/admin/labs/light/${action}?LabId=${labId}&DeviceId=${deviceId}`, {})
      .pipe(map(() => void 0));
  }

  setFanState(labId: number, on: boolean, deviceId = 1): Observable<void> {
    const action = on ? 'on' : 'off';
    return this.http
      .post(`${this.api}/admin/labs/fan/${action}?LabId=${labId}&DeviceId=${deviceId}`, {})
      .pipe(map(() => void 0));
  }

  /** Dashboard: labs + device stats + charts + alerts from API. */
  getDashboardData(): Observable<DashboardVm> {
    if (this.auth.isAdmin()) {
      return this.getAdminLabsOverview().pipe(
        switchMap((overview) => {
          if (overview.length === 0) {
            return of(this.emptyDashboard([]));
          }
          const deviceCalls = overview.map((o) =>
            this.getLabDevices(o.id).pipe(catchError(() => of([] as LabDeviceDto[])))
          );
          return forkJoin(deviceCalls).pipe(
            map((deviceLists) => this.buildAdminDashboard(overview, deviceLists))
          );
        })
      );
    }
    if (this.auth.isStaff()) {
      return this.http.get<CurrentLabApiRow[]>(`${this.api}/staff/me/current-labs`).pipe(
        map((rows) => this.buildStaffDashboard(rows))
      );
    }
    return of(this.emptyDashboard([]));
  }

  mapDeviceDto(d: LabDeviceDto): Device {
    const icon = deviceTypeToIcon(d.deviceType);
    const statusLabel = d.isActive ? ('ON' as const) : ('OFF' as const);
    const runStatus =
      d.lastSeenAt && minutesAgo(d.lastSeenAt) < 30 ? ('RUNNING' as const) : ('NO USAGE' as const);
    return {
      id: String(d.id),
      name: d.name,
      icon,
      statusLabel,
      isOn: d.isActive,
      kwh: '—',
      runStatus,
    };
  }

  private emptyDashboard(labs: Lab[]): DashboardVm {
    return {
      labs,
      totalLabs: labs.length,
      activeLabCount: 0,
      idleLabCount: 0,
      offLabCount: 0,
      displayKwhTotal: 0,
      displayKwhSaved: 0,
      deviceTotal: 0,
      deviceActive: 0,
      deviceIdleOrOff: 0,
      labUsage: [],
      weekData: weekPlaceholder(0),
      alerts: [
        {
          id: 1,
          icon: 'light',
          title: 'No labs yet',
          desc: 'Seed the database or create labs in the admin API.',
          time: '—',
        },
      ],
      activities: [],
    };
  }

  private buildAdminDashboard(
    overview: LabOverviewApiRow[],
    deviceLists: LabDeviceDto[][]
  ): DashboardVm {
    const labs = overview.map((r) => this.mapOverviewToLab(r));
    const allDevices = deviceLists.flat();
    const deviceActive = allDevices.filter((d) => d.isActive).length;
    const deviceTotal = allDevices.length;
    const deviceIdleOrOff = Math.max(0, deviceTotal - deviceActive);

    const activeLabCount = overview.filter((o) => o.inSession).length;
    const idleLabCount = overview.filter((o) => !o.inSession).length;
    const displayKwhTotal = Math.round(
      overview.reduce((s, o) => s + o.lastOccupancyCount * 2.4 + (o.inSession ? 6 : 0), 0)
    );
    const displayKwhSaved = Math.round(displayKwhTotal * 0.28);

    const labUsage = overview.map((o) => ({
      lab: truncateName(o.name, 10),
      val: Math.min(24, Math.round(o.lastOccupancyCount * 2.2 + (o.inSession ? 5 : 1))),
    }));

    const base = Math.max(2, Math.round(displayKwhTotal / Math.max(1, overview.length)));
    const weekData = weekPlaceholder(base);

    return {
      labs,
      totalLabs: overview.length,
      activeLabCount,
      idleLabCount,
      offLabCount: 0,
      displayKwhTotal,
      displayKwhSaved,
      deviceTotal,
      deviceActive,
      deviceIdleOrOff,
      labUsage,
      weekData,
      alerts: this.buildAlertsFromOverview(overview),
      activities: this.buildActivitiesFromOverview(overview),
    };
  }

  private buildStaffDashboard(rows: CurrentLabApiRow[]): DashboardVm {
    const labs = rows.map((r) => this.mapStaffLab(r));
    const activeLabCount = labs.filter((l) => l.status === 'Active').length;
    const idleLabCount = labs.filter((l) => l.status === 'Idle').length;
    const displayKwhTotal = Math.round(rows.reduce((s, r) => s + (r.inSession ? 14 : 5), 0));
    const labUsage = rows.map((r) => ({
      lab: truncateName(r.name, 10),
      val: Math.min(24, r.inSession ? 16 : 6),
    }));
    return {
      labs,
      totalLabs: rows.length,
      activeLabCount,
      idleLabCount,
      offLabCount: 0,
      displayKwhTotal,
      displayKwhSaved: Math.round(displayKwhTotal * 0.22),
      deviceTotal: 0,
      deviceActive: 0,
      deviceIdleOrOff: 0,
      labUsage,
      weekData: weekPlaceholder(Math.max(3, Math.round(displayKwhTotal / 2))),
      alerts: this.buildStaffAlerts(rows),
      activities: rows.map((r, i) => ({
        color: r.inSession ? '#22c55e' : '#94a3b8',
        text: `${r.name}: ${r.inSession ? 'In scheduled session' : 'Outside session'}`,
        time: 'Live',
      })),
    };
  }

  private buildAlertsFromOverview(overview: LabOverviewApiRow[]): Alert[] {
    const out: Alert[] = [];
    let nid = 1;
    for (const o of overview) {
      if (!o.inSession && o.lastOccupancyCount > 0) {
        out.push({
          id: nid++,
          icon: 'bolt',
          title: `Occupancy outside session — ${o.name}`,
          desc: `Last crowd count: ${o.lastOccupancyCount}. Check schedule or overrides.`,
          time: formatRelativeIso(o.lastOccupancyAt),
        });
      }
    }
    if (out.length === 0) {
      out.push({
        id: 1,
        icon: 'light',
        title: 'No occupancy anomalies',
        desc: 'No labs reported people on site while out of session.',
        time: '—',
      });
    }
    return out.slice(0, 6);
  }

  private buildStaffAlerts(rows: CurrentLabApiRow[]): Alert[] {
    if (rows.length === 0) {
      return [
        {
          id: 1,
          icon: 'light',
          title: 'No assigned labs',
          desc: 'You have no current lab assignments for this time window.',
          time: '—',
        },
      ];
    }
    return rows.map((r, i) => ({
      id: i + 1,
      icon: r.inSession ? 'light' : 'bolt',
      title: r.inSession ? `${r.name} — session active` : `${r.name} — idle window`,
      desc: r.inSession ? 'Scheduled section is in progress.' : 'No active section for this lab now.',
      time: 'Live',
    }));
  }

  private buildActivitiesFromOverview(overview: LabOverviewApiRow[]): Activity[] {
    return overview.map((o) => ({
      color: o.inSession ? '#22c55e' : '#64748b',
      text: `${o.name}: occupancy ${o.lastOccupancyCount} · ${o.inSession ? 'In session' : 'Out of session'}`,
      time: formatRelativeIso(o.lastOccupancyAt),
    }));
  }

  private mapOverviewToLab(o: LabOverviewApiRow): Lab {
    const ov = readLabOverride(o);
    const status: LabStatus = o.inSession ? 'Active' : 'Idle';
    const lights = ov?.forceLightOn === true ? 'ON' : ov?.forceLightOn === false ? 'OFF' : 'IDLE';
    const ac = ov?.forceFanOn === true ? 'ON' : ov?.forceFanOn === false ? 'OFF' : 'IDLE';
    const energyUsed = Math.min(
      29,
      Math.round(o.lastOccupancyCount * 2.2 + (o.inSession ? 7 : 2))
    );
    return {
      id: String(o.id),
      name: o.name,
      floor: '—',
      status,
      energyUsed,
      energyMax: 30,
      ac,
      lights,
      pcs: 'IDLE',
      lastUpdate: formatRelativeIso(o.lastOccupancyAt) || `People: ${o.lastOccupancyCount}`,
    };
  }

  private mapStaffLab(r: CurrentLabApiRow): Lab {
    const ov = readLabOverride(r);
    const status: LabStatus = r.inSession ? 'Active' : 'Idle';
    const lights = ov?.forceLightOn === true ? 'ON' : ov?.forceLightOn === false ? 'OFF' : 'IDLE';
    const ac = ov?.forceFanOn === true ? 'ON' : ov?.forceFanOn === false ? 'OFF' : 'IDLE';
    return {
      id: String(r.id),
      name: r.name,
      floor: '—',
      status,
      energyUsed: r.inSession ? 12 : 4,
      energyMax: 30,
      ac,
      lights,
      pcs: 'IDLE',
      lastUpdate: ov ? 'Override active' : r.inSession ? 'In session' : 'Out of session',
    };
  }
}

function deviceTypeToIcon(t: string): string {
  const m: Record<string, string> = {
    Esp32Controller: 'pcs',
    MotionSensor: 'monitor',
    TemperatureSensor: 'ac',
    HumiditySensor: 'ac',
    LightSensor: 'light',
    RelayModule: 'light',
    Camera: 'monitor',
  };
  return m[t] ?? 'monitor';
}

function minutesAgo(iso: string): number {
  const t = new Date(iso).getTime();
  if (Number.isNaN(t)) return 9999;
  return (Date.now() - t) / 60000;
}

function truncateName(name: string, max: number): string {
  const t = name.trim();
  return t.length <= max ? t : t.slice(0, max - 1) + '…';
}

function formatRelativeIso(iso: string | null): string {
  if (!iso) return '';
  const t = new Date(iso).getTime();
  if (Number.isNaN(t)) return '';
  const mins = Math.round((Date.now() - t) / 60000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins} min ago`;
  const hrs = Math.round(mins / 60);
  if (hrs < 24) return `${hrs} hr ago`;
  return `${Math.round(hrs / 24)} d ago`;
}

function weekPlaceholder(base: number): { day: string; val: number }[] {
  const days = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];
  return days.map((day, i) => ({
    day,
    val: Math.min(24, Math.max(1, base + i * 2 - 4)),
  }));
}
