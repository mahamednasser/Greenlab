import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { authGuard } from './core/auth.guard';
import { adminGuard } from './core/admin.guard';

export const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login',    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./pages/register/register.component').then(m => m.RegisterComponent) },
    ]
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'labs',      loadComponent: () => import('./pages/labs/labs.component').then(m => m.LabsComponent) },
      { path: 'labs/:id',  loadComponent: () => import('./pages/lab-detail/lab-detail.component').then(m => m.LabDetailComponent) },
      { path: 'staff/lab-change', loadComponent: () => import('./pages/staff/lab-change/lab-change.component').then(m => m.LabChangeComponent) },
      { path: 'admin/users',    loadComponent: () => import('./pages/admin/users/users.component').then(m => m.UsersComponent), canActivate: [adminGuard] },
      { path: 'admin/devices',  loadComponent: () => import('./pages/admin/devices/devices.component').then(m => m.DevicesComponent), canActivate: [adminGuard] },
      { path: 'admin/smart-acs', loadComponent: () => import('./pages/admin/smart-acs/smart-acs.component').then(m => m.SmartAcsComponent), canActivate: [adminGuard] },
      { path: 'admin/courses',  loadComponent: () => import('./pages/admin/courses/courses.component').then(m => m.CoursesComponent), canActivate: [adminGuard] },
      { path: 'admin/staff',    loadComponent: () => import('./pages/admin/staff/staff.component').then(m => m.StaffComponent), canActivate: [adminGuard] },
      { path: 'admin/lab-change-requests', loadComponent: () => import('./pages/admin/lab-change-requests/lab-change-requests.component').then(m => m.LabChangeRequestsComponent), canActivate: [adminGuard] },
      { path: 'admin/labs/new', loadComponent: () => import('./pages/admin/lab-form/lab-form.component').then(m => m.LabFormComponent), canActivate: [adminGuard] },
      { path: 'admin/labs/:id/edit', loadComponent: () => import('./pages/admin/lab-form/lab-form.component').then(m => m.LabFormComponent), canActivate: [adminGuard] },
      { path: 'admin/audit-logs', loadComponent: () => import('./pages/admin/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent), canActivate: [adminGuard] },
      { path: 'alerts',    redirectTo: 'dashboard' },
      { path: 'settings',  redirectTo: 'dashboard' },
    ]
  },
  { path: '**', redirectTo: 'login' }
];
