export type LabStatus = 'Active' | 'Idle' | 'Off';
export type DeviceRunStatus = 'RUNNING' | 'IDLE' | 'NO USAGE';

export interface Lab {
  id: string; name: string; floor: string; status: LabStatus;
  energyUsed: number; energyMax: number;
  ac: string; lights: string; pcs: string; lastUpdate: string;
}

export interface Device {
  id: string; name: string; icon: string;
  statusLabel: 'ON' | 'IDLE' | 'OFF';
  isOn: boolean; kwh: string; runStatus: DeviceRunStatus;
}

export interface Alert { id: number; icon: string; title: string; desc: string; time: string; }
export interface Activity { color: string; text: string; time: string; }
