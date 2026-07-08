import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminService } from '../../../services/admin.service';
import { DeviceWithLab } from '../../../models/api.types';

@Component({
  selector: 'app-devices',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './devices.component.html',
  styleUrls: ['../admin.shared.scss', './devices.component.scss'],
})
export class DevicesComponent implements OnInit {
  devices: DeviceWithLab[] = [];
  loading = true;
  errorMsg: string | null = null;

  constructor(private readonly admin: AdminService) {}

  ngOnInit(): void {
    this.fetch();
  }

  private fetch(): void {
    this.loading = true;
    this.errorMsg = null;
    this.admin.getAllDevices().subscribe({
      next: (rows) => {
        this.devices = rows;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'ADMIN.DEVICES.ERROR';
      },
    });
  }

  typeKey(type: string): string {
    return `ADMIN.DEVICE_TYPES.${type.toUpperCase()}`;
  }
}
