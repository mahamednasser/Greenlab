export type LabStatus = 'Active' | 'Idle' | 'Off';
export type DeviceRunStatus = 'RUNNING' | 'IDLE' | 'NO USAGE';

export interface Lab {
  id: string; name: string; floor: string; status: LabStatus;
  /** Instantaneous watts (light 20 + fan 70). */
  energyUsed: number;
  energyMax: number;
  /** Accumulated energy from ON runtime (kWh). */
  energyKwh: number;
  lightRuntimeSeconds: number;
  fanRuntimeSeconds: number;
  ac: string; lights: string; pcs: string; lastUpdate: string;
  peopleCount: number;
}

export interface Device {
  id: string; name: string; icon: string;
  statusLabel: 'ON' | 'IDLE' | 'OFF';
  isOn: boolean; kwh: string; runStatus: DeviceRunStatus;
}

export interface Alert { id: number; icon: string; title: string; desc: string; time: string; }
export interface Activity { color: string; text: string; time: string; }
