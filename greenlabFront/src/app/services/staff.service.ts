import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateLabChangeRequestDto,
  CurrentLabApiRow,
  LabChangeRequestDto,
  LabOptionDto,
} from '../models/api.types';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly api = '/api/staff';

  constructor(private readonly http: HttpClient) {}

  getCurrentLabs(): Observable<CurrentLabApiRow[]> {
    return this.http.get<CurrentLabApiRow[]>(`${this.api}/me/current-labs`);
  }

  listLabs(): Observable<LabOptionDto[]> {
    return this.http.get<LabOptionDto[]>(`${this.api}/labs`);
  }

  getMyLabChangeRequests(): Observable<LabChangeRequestDto[]> {
    return this.http.get<LabChangeRequestDto[]>(`${this.api}/lab-change-requests/mine`);
  }

  createLabChangeRequest(body: CreateLabChangeRequestDto): Observable<number> {
    return this.http.post<number>(`${this.api}/lab-change-requests`, body);
  }
}
