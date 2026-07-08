/** Matches Greenlab.Application.DTOs.LoginResponse */
export interface LoginResponse {
  token: string;
  email: string;
  fullName: string;
  role: string;
  expiresAt: string;
}

/** Matches LabDetailDto (camelCase JSON) */
export interface LabDetailDto {
  id: number;
  name: string;
  deviceKey: string;
  lightThreshold: number;
  tempFanOnThresholdCelsius: number;
  cvLabCode: string | null;
  cameraRtspUrl: string | null;
}

export interface LabOverrideDto {
  motionSensorDisabled: boolean;
  forceLightOn: boolean | null;
  forceFanOn: boolean | null;
  forceRelay3On: boolean | null;
  forceRelay4On: boolean | null;
}

/** Staff current-labs row; `override` comes from API as camelCase */
export type CurrentLabApiRow = {
  id: number;
  name: string;
  inSession: boolean;
} & Record<string, unknown>;

export function readLabOverride(row: Record<string, unknown>): LabOverrideDto | null {
  const v = row['override'];
  if (v == null || typeof v !== 'object') return null;
  return v as LabOverrideDto;
}

/** GET /api/admin/labs/overview row (camelCase JSON; `override` via readLabOverride). */
export type LabOverviewApiRow = {
  id: number;
  name: string;
  lastOccupancyCount: number;
  lastOccupancyAt: string | null;
  inSession: boolean;
} & Record<string, unknown>;

/** LabDeviceDto */
export interface LabDeviceDto {
  id: number;
  labId: number;
  name: string;
  deviceType: string;
  ipAddress: string | null;
  isActive: boolean;
  lastSeenAt: string | null;
  createdAt: string;
}

export interface DeviceWithLab extends LabDeviceDto {
  labName: string;
}

export interface UserListItem {
  id: number;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  password: string;
  role: string;
  isActive: boolean;
}

export interface UpdateUserRequest {
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  password?: string;
}

export interface LabSectionRow {
  id: number;
  labId: number;
  labName: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courseName: string | null;
}

export interface CreateLabRequest {
  name: string;
  deviceKey?: string;
  lightThreshold?: number;
  tempFanOnThresholdCelsius?: number;
  cvLabCode?: string;
  cameraRtspUrl?: string;
}

export interface LabSectionSlot {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courseName?: string;
}

export interface StaffAssignmentRow {
  id: number;
  userId: number;
  staffName: string;
  staffEmail: string;
  staffRole: string;
  labId: number;
  labName: string;
  dayOfWeek: number | null;
  startTime: string | null;
  endTime: string | null;
  validFrom: string | null;
  validUntil: string | null;
  assignmentSource: string;
  isActive: boolean;
}

export interface CreateStaffAssignmentRequest {
  staffEmail: string;
  labId: number;
  dayOfWeek?: number;
  startTime?: string;
  endTime?: string;
  validFrom?: string;
  validUntil?: string;
}
