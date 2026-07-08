import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { LabDetailDto, LabSectionRow } from '../../../models/api.types';

const DAY_KEYS = ['SUNDAY', 'MONDAY', 'TUESDAY', 'WEDNESDAY', 'THURSDAY', 'FRIDAY', 'SATURDAY'];

@Component({
  selector: 'app-courses',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './courses.component.html',
  styleUrls: ['../admin.shared.scss', './courses.component.scss'],
})
export class CoursesComponent implements OnInit {
  sections: LabSectionRow[] = [];
  labs: LabDetailDto[] = [];
  loading = true;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  saving = false;

  showForm = false;
  labId = 0;
  dayOfWeek = 1;
  startTime = '09:00';
  endTime = '10:30';
  courseName = '';

  readonly dayOptions = DAY_KEYS.map((key, i) => ({ value: i, key }));

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
    this.admin.listLabs().subscribe({
      next: (labs) => {
        this.labs = labs;
        if (labs.length && !this.labId) this.labId = labs[0].id;
      },
    });
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.getSections().subscribe({
      next: (rows) => {
        this.sections = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.COURSES.ERROR';
      },
    });
  }

  dayLabel(day: number): string {
    return DAY_KEYS[day] ?? 'MONDAY';
  }

  formatTime(value: string): string {
    if (!value) return '—';
    return value.length >= 5 ? value.slice(0, 5) : value;
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    this.successMsg = null;
  }

  submitSection(): void {
    if (!this.labId || !this.courseName.trim()) return;
    this.saving = true;
    this.errorMsg = null;
    this.successMsg = null;
    this.admin
      .addSections(this.labId, [
        {
          dayOfWeek: this.dayOfWeek,
          startTime: `${this.startTime}:00`,
          endTime: `${this.endTime}:00`,
          courseName: this.courseName.trim(),
        },
      ])
      .subscribe({
        next: () => {
          this.saving = false;
          this.successMsg = 'ADMIN.COURSES.SAVED';
          this.courseName = '';
          this.showForm = false;
          this.fetch();
        },
        error: () => {
          this.saving = false;
          this.errorMsg = 'ADMIN.COURSES.SAVE_ERROR';
        },
      });
  }
}
