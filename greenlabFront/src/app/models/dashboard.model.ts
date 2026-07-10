import { Lab, Alert, Activity } from './lab.model';

/** Aggregated UI state for the dashboard (runtime-based energy). */
export interface DashboardVm {
  labs: Lab[];
  totalLabs: number;
  activeLabCount: number;
  idleLabCount: number;
  offLabCount: number;
  /** Sum of current light (20W) + fan (70W) loads across labs. */
  totalWatts: number;
  /** Watts currently not drawn because loads are OFF. */
  savedWatts: number;
  /** Energy from ON runtime: Σ (lightH×0.02 + fanH×0.07) kWh. */
  displayKwhTotal: number;
  /** Estimated saved kWh if OFF loads had stayed off for the same runtimes (not used as primary). */
  displayKwhSaved: number;
  /** Total light ON seconds across labs. */
  lightRuntimeSeconds: number;
  /** Total fan ON seconds across labs. */
  fanRuntimeSeconds: number;
  deviceTotal: number;
  deviceActive: number;
  deviceIdleOrOff: number;
  /** Per-lab kWh from runtime. */
  labUsage: { lab: string; val: number }[];
  weekData: { day: string; val: number }[];
  alerts: Alert[];
  activities: Activity[];
}


