import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';

@Component({
  selector: 'app-lab-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './lab-form.component.html',
  styleUrls: ['../admin.shared.scss', './lab-form.component.scss'],
})
export class LabFormComponent {
  name = '';
  deviceKey = '';
  lightThreshold = 2000;
  tempFanOnThresholdCelsius = 28;
  cvLabCode = '';
  cameraRtspUrl = '';

  saving = false;
  errorMsg: string | null = null;

  constructor(
    private readonly admin: AdminService,
    private readonly router: Router
  ) {}

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

    this.admin.createLab(body).subscribe({
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
