import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import {
  CreateLabRequest,
  DeviceWithLab,
  LabDetailDto,
  LabDeviceDto,
  LabSectionRow,
  LabSectionSlot,
  UserListItem,
  CreateUserRequest,
  UpdateUserRequest,
  StaffAssignmentRow,
  CreateStaffAssignmentRequest,
  AuditLogDto,
  LabChangeRequestDto,
  ReviewLabChangeRequestDto,
} from '../models/api.types';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = '/api/admin';

  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(`${this.api}/users`);
  }

  createUser(body: CreateUserRequest): Observable<UserListItem> {
    return this.http.post<UserListItem>(`${this.api}/users`, body);
  }

  updateUser(id: number, body: UpdateUserRequest): Observable<UserListItem> {
    return this.http.put<UserListItem>(`${this.api}/users/${id}`, body);
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/users/${id}`);
  }

  getSections(): Observable<LabSectionRow[]> {
    return this.http.get<LabSectionRow[]>(`${this.api}/sections`);
  }

  listLabs(): Observable<LabDetailDto[]> {
    return this.http.get<LabDetailDto[]>(`${this.api}/labs`);
  }

  getLabDevices(labId: number): Observable<LabDeviceDto[]> {
    return this.http.get<LabDeviceDto[]>(`${this.api}/labs/${labId}/devices`);
  }

  getAllDevices(): Observable<DeviceWithLab[]> {
    return this.listLabs().pipe(
      switchMap((labs) => {
        if (labs.length === 0) return of([] as DeviceWithLab[]);
        return forkJoin(
          labs.map((lab) =>
            this.getLabDevices(lab.id).pipe(
              catchError(() => of([] as LabDeviceDto[])),
              map((devices) =>
                devices.map((d) => ({ ...d, labName: lab.name }))
              )
            )
          )
        ).pipe(map((lists) => lists.flat()));
      })
    );
  }

  createLab(body: CreateLabRequest): Observable<LabDetailDto> {
    return this.http.post<LabDetailDto>(`${this.api}/labs`, body);
  }

  addSections(labId: number, sections: LabSectionSlot[]): Observable<void> {
    return this.http
      .post(`${this.api}/labs/${labId}/sections`, { sections }, { responseType: 'text' })
      .pipe(map(() => void 0));
  }

  getStaffAssignments(): Observable<StaffAssignmentRow[]> {
    return this.http.get<StaffAssignmentRow[]>(`${this.api}/staff-assignments`);
  }

  createStaffAssignment(body: CreateStaffAssignmentRequest): Observable<StaffAssignmentRow> {
    return this.http.post<StaffAssignmentRow>(`${this.api}/staff-assignments`, body);
  }

  updateStaffAssignment(id: number, body: CreateStaffAssignmentRequest): Observable<StaffAssignmentRow> {
    return this.http.put<StaffAssignmentRow>(`${this.api}/staff-assignments/${id}`, body);
  }

  deleteStaffAssignment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/staff-assignments/${id}`);
  }

  getAuditLogs(): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>(`${this.api}/audit-logs`);
  }

  getLabChangeRequests(status?: string): Observable<LabChangeRequestDto[]> {
    const params = status ? `?status=${encodeURIComponent(status)}` : '';
    return this.http.get<LabChangeRequestDto[]>(`${this.api}/lab-change-requests${params}`);
  }

  reviewLabChangeRequest(id: number, body: ReviewLabChangeRequestDto): Observable<void> {
    return this.http.post<void>(`${this.api}/lab-change-requests/${id}/review`, body);
  }

  updateLab(id: number, body: CreateLabRequest): Observable<LabDetailDto> {
    return this.http.put<LabDetailDto>(`${this.api}/labs/${id}`, body);
  }

  deleteLab(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/labs/${id}`);
  }
}
