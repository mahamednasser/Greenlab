import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { LabService } from '../../../services/lab.service';

@Component({
  selector: 'app-lab-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './lab-form.component.html',
  styleUrls: ['../admin.shared.scss', './lab-form.component.scss'],
})
export class LabFormComponent implements OnInit {
  name = '';
  deviceKey = '';
  lightThreshold = 2000;
  tempFanOnThresholdCelsius = 28;
  cvLabCode = '';
  cameraRtspUrl = '';

  isEdit = false;
  labId = NaN;
  saving = false;
  errorMsg: string | null = null;

  constructor(
    private readonly admin: AdminService,
    private readonly labSvc: LabService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.labId = parseInt(idParam, 10);
      if (!Number.isNaN(this.labId)) {
        this.isEdit = true;
        this.loadLab();
      }
    }
  }

  private loadLab(): void {
    this.saving = true;
    this.labSvc.getLabDetail(this.labId).subscribe({
      next: (lab) => {
        this.saving = false;
        if (lab) {
          this.name = lab.name;
          this.deviceKey = lab.deviceKey || '';
          this.lightThreshold = lab.lightThreshold;
          this.tempFanOnThresholdCelsius = lab.tempFanOnThresholdCelsius;
          this.cvLabCode = lab.cvLabCode || '';
          this.cameraRtspUrl = lab.cameraRtspUrl || '';
        }
      },
      error: () => {
        this.saving = false;
        this.errorMsg = 'ADMIN.LAB_FORM.ERROR';
      },
    });
  }

  submit(): void {
    if (!this.name.trim()) return;
    this.saving = true;
    this.errorMsg = null;

    const body = {
      name: this.name.trim(),
      lightThreshold: this.lightThreshold,
      tempFanOnThresholdCelsius: this.tempFanOnThresholdCelsius,
      cvLabCode: this.cvLabCode.trim() || undefined,
      cameraRtspUrl: this.cameraRtspUrl.trim() || undefined,
      ...(this.deviceKey.trim() ? { deviceKey: this.deviceKey.trim() } : {}),
    };

    const obs = this.isEdit
      ? this.admin.updateLab(this.labId, body)
      : this.admin.createLab(body);

    obs.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigate(['/labs']);
      },
      error: () => {
        this.saving = false;
        this.errorMsg = 'ADMIN.LAB_FORM.ERROR';
      },
    });
  }

  deleteLab(): void {
    const msg = this.translate.instant('ADMIN.LAB_FORM.DELETE_CONFIRM', { name: this.name }) 
                || `Are you sure you want to delete lab "${this.name}"?`;
    if (!confirm(msg)) return;
    this.saving = true;
    this.admin.deleteLab(this.labId).subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigate(['/labs']);
      },
      error: () => {
        this.saving = false;
        this.errorMsg = 'ADMIN.LAB_FORM.ERROR';
      },
    });
  }
}
