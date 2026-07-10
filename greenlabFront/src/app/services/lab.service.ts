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
import {
  FAN_WATTS,
  LAB_MAX_WATTS,
  LIGHT_WATTS,
  energyFromRuntime,
  formatRuntime,
  labSavedWatts,
  labWatts,
} from '../utils/power';

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
    const base = this.auth.isAdmin() ? `${this.api}/admin/labs` : `${this.api}/staff/labs`;
    return this.http
      .post(`${base}/light/${action}?LabId=${labId}&DeviceId=${deviceId}`, {})
      .pipe(map(() => void 0));
  }

  setFanState(labId: number, on: boolean, deviceId = 1): Observable<void> {
    const action = on ? 'on' : 'off';
    const base = this.auth.isAdmin() ? `${this.api}/admin/labs` : `${this.api}/staff/labs`;
    return this.http
      .post(`${base}/fan/${action}?LabId=${labId}&DeviceId=${deviceId}`, {})
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
      totalWatts: 0,
      savedWatts: 0,
      displayKwhTotal: 0,
      displayKwhSaved: 0,
      lightRuntimeSeconds: 0,
      fanRuntimeSeconds: 0,
      deviceTotal: 0,
      deviceActive: 0,
      deviceIdleOrOff: 0,
      labUsage: [],
      weekData: buildWeekSeries(0),
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

  private powerTotals(labs: Lab[]): Pick<
    DashboardVm,
    | 'totalWatts'
    | 'savedWatts'
    | 'displayKwhTotal'
    | 'displayKwhSaved'
    | 'lightRuntimeSeconds'
    | 'fanRuntimeSeconds'
    | 'labUsage'
    | 'weekData'
  > {
    const totalWatts = labs.reduce(
      (s, l) => s + labWatts(l.lights === 'ON', l.ac === 'ON'),
      0
    );
    const savedWatts = labs.reduce(
      (s, l) => s + labSavedWatts(l.lights === 'ON', l.ac === 'ON'),
      0
    );
    const lightRuntimeSeconds = labs.reduce((s, l) => s + (l.lightRuntimeSeconds || 0), 0);
    const fanRuntimeSeconds = labs.reduce((s, l) => s + (l.fanRuntimeSeconds || 0), 0);
    const displayKwhTotal = labs.reduce((s, l) => s + (l.energyKwh || 0), 0);
    const labUsage = labs.map((l) => ({
      lab: truncateName(l.name, 10),
      val: Math.round((l.energyKwh || 0) * 10000) / 10000,
    }));
    return {
      totalWatts,
      savedWatts,
      displayKwhTotal: Math.round(displayKwhTotal * 10000) / 10000,
      displayKwhSaved: 0,
      lightRuntimeSeconds,
      fanRuntimeSeconds,
      labUsage,
      weekData: buildWeekSeries(displayKwhTotal),
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
    const power = this.powerTotals(labs);

    return {
      labs,
      totalLabs: overview.length,
      activeLabCount,
      idleLabCount,
      offLabCount: 0,
      ...power,
      deviceTotal,
      deviceActive,
      deviceIdleOrOff,
      alerts: this.buildAlertsFromOverview(overview),
      activities: this.buildActivitiesFromOverview(overview),
    };
  }

  private buildStaffDashboard(rows: CurrentLabApiRow[]): DashboardVm {
    const labs = rows.map((r) => this.mapStaffLab(r));
    const activeLabCount = labs.filter((l) => l.status === 'Active').length;
    const idleLabCount = labs.filter((l) => l.status === 'Idle').length;
    const power = this.powerTotals(labs);

    // Count ON loads as "device" activity for staff (light + fan per lab).
    const loadsOn = labs.reduce(
      (n, l) => n + (l.lights === 'ON' ? 1 : 0) + (l.ac === 'ON' ? 1 : 0),
      0
    );
    const loadsTotal = labs.length * 2;

    return {
      labs,
      totalLabs: rows.length,
      activeLabCount,
      idleLabCount,
      offLabCount: 0,
      ...power,
      deviceTotal: loadsTotal,
      deviceActive: loadsOn,
      deviceIdleOrOff: Math.max(0, loadsTotal - loadsOn),
      alerts: this.buildStaffAlerts(rows),
      activities: rows.map((r) => {
        const lightSec = Number(r.lightRuntimeSeconds ?? 0);
        const fanSec = Number(r.fanRuntimeSeconds ?? 0);
        const kwh =
          Number(r.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
        return {
          color: r.inSession ? '#22c55e' : '#94a3b8',
          text: `${r.name}: ${kwh.toFixed(3)} kWh · light ${formatRuntime(lightSec)} · fan ${formatRuntime(fanSec)}`,
          time: 'Live',
        };
      }),
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
      const lightSec = Number(o.lightRuntimeSeconds ?? 0);
      const fanSec = Number(o.fanRuntimeSeconds ?? 0);
      const kwh = Number(o.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
      if (kwh > 0 || o.actualLightOn || o.actualFanOn) {
        out.push({
          id: nid++,
          icon: 'light',
          title: `${o.name}: ${kwh.toFixed(3)} kWh`,
          desc: `Light ${formatRuntime(lightSec)} @ ${LIGHT_WATTS}W · Fan ${formatRuntime(fanSec)} @ ${FAN_WATTS}W`,
          time: formatRelativeIso(o.lastOccupancyAt) || 'Live',
        });
      }
    }
    if (out.length === 0) {
      out.push({
        id: 1,
        icon: 'light',
        title: 'No energy consumed yet',
        desc: `Turn on light (${LIGHT_WATTS}W) or fan (${FAN_WATTS}W) to accumulate kWh from runtime.`,
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
    return rows.map((r, i) => {
      const lightSec = Number(r.lightRuntimeSeconds ?? 0);
      const fanSec = Number(r.fanRuntimeSeconds ?? 0);
      const kwh = Number(r.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
      return {
        id: i + 1,
        icon: kwh > 0 ? 'bolt' : 'light',
        title: `${r.name} — ${kwh.toFixed(3)} kWh`,
        desc: `Light ${formatRuntime(lightSec)} @ ${LIGHT_WATTS}W · Fan ${formatRuntime(fanSec)} @ ${FAN_WATTS}W`,
        time: 'Live',
      };
    });
  }

  private buildActivitiesFromOverview(overview: LabOverviewApiRow[]): Activity[] {
    return overview.map((o) => {
      const lightSec = Number(o.lightRuntimeSeconds ?? 0);
      const fanSec = Number(o.fanRuntimeSeconds ?? 0);
      const kwh = Number(o.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
      return {
        color: kwh > 0 ? '#22c55e' : '#64748b',
        text: `${o.name}: ${kwh.toFixed(3)} kWh · L ${formatRuntime(lightSec)} · F ${formatRuntime(fanSec)} · people ${o.lastOccupancyCount}`,
        time: formatRelativeIso(o.lastOccupancyAt),
      };
    });
  }

  private mapOverviewToLab(o: LabOverviewApiRow): Lab {
    const status: LabStatus = o.inSession ? 'Active' : o.actualLightOn || o.actualFanOn ? 'Idle' : 'Off';
    const lights = o.actualLightOn ? 'ON' : 'OFF';
    const ac = o.actualFanOn ? 'ON' : 'OFF';
    const lightSec = Number(o.lightRuntimeSeconds ?? 0);
    const fanSec = Number(o.fanRuntimeSeconds ?? 0);
    const energyUsed = Number(o.currentWatts ?? 0) || labWatts(!!o.actualLightOn, !!o.actualFanOn);
    const energyKwh = Number(o.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec);
    return {
      id: String(o.id),
      name: o.name,
      floor: '—',
      status,
      energyUsed,
      energyMax: LAB_MAX_WATTS,
      energyKwh,
      lightRuntimeSeconds: lightSec,
      fanRuntimeSeconds: fanSec,
      ac,
      lights,
      pcs: 'IDLE',
      lastUpdate: formatRelativeIso(o.lastOccupancyAt) || `People: ${o.lastOccupancyCount}`,
      peopleCount: o.lastOccupancyCount,
    };
  }

  private mapStaffLab(r: CurrentLabApiRow): Lab {
    const lights = r.actualLightOn ? 'ON' : 'OFF';
    const ac = r.actualFanOn ? 'ON' : 'OFF';
    const status: LabStatus = r.inSession ? 'Active' : r.actualLightOn || r.actualFanOn ? 'Idle' : 'Off';
    const ov = readLabOverride(r);
    const lastOccupancy = (r['lastOccupancyCount'] as number) || 0;
    const lightSec = Number(r.lightRuntimeSeconds ?? 0);
    const fanSec = Number(r.fanRuntimeSeconds ?? 0);
    return {
      id: String(r.id),
      name: r.name,
      floor: '—',
      status,
      energyUsed: Number(r.currentWatts ?? 0) || labWatts(!!r.actualLightOn, !!r.actualFanOn),
      energyMax: LAB_MAX_WATTS,
      energyKwh: Number(r.energyKwh ?? 0) || energyFromRuntime(lightSec, fanSec),
      lightRuntimeSeconds: lightSec,
      fanRuntimeSeconds: fanSec,
      ac,
      lights,
      pcs: 'IDLE',
      lastUpdate: ov ? 'Override active' : r.inSession ? 'In session' : 'Out of session',
      peopleCount: lastOccupancy,
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

/** Synthetic week series anchored on today's measured energy (kWh). */
function buildWeekSeries(todayKwh: number): { day: string; val: number }[] {
  const days = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];
  const todayIdx = (new Date().getDay() + 6) % 7; // Monday=0
  const factors = [0.72, 0.8, 0.88, 0.95, 1, 0.55, 0.4];
  return days.map((day, i) => {
    if (i === todayIdx) return { day, val: todayKwh };
    return { day, val: Math.round(todayKwh * factors[i] * 10000) / 10000 };
  });
}
