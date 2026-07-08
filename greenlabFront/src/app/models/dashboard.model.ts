import { Lab, Alert, Activity } from './lab.model';

/** Aggregated UI state for the dashboard (from API + light client math). */
export interface DashboardVm {
  labs: Lab[];
  totalLabs: number;
  activeLabCount: number;
  idleLabCount: number;
  offLabCount: number;
  /** Display metric derived from occupancy (not real billing kWh). */
  displayKwhTotal: number;
  displayKwhSaved: number;
  deviceTotal: number;
  deviceActive: number;
  deviceIdleOrOff: number;
  labUsage: { lab: string; val: number }[];
  weekData: { day: string; val: number }[];
  alerts: Alert[];
  activities: Activity[];
}
