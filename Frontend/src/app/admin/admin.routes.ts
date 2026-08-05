import { Routes } from '@angular/router';
import { adminGuard } from './admin.guard';

export const adminRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login/admin-login.component').then(m => m.AdminLoginComponent)
  },
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./shell/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () => import('./overview/admin-overview.component').then(m => m.AdminOverviewComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./users/admin-users.component').then(m => m.AdminUsersComponent)
      },
      {
        path: 'sms',
        loadComponent: () => import('./sms/admin-sms.component').then(m => m.AdminSmsComponent)
      },
      {
        path: 'logs',
        loadComponent: () => import('./logs/admin-logs.component').then(m => m.AdminLogsComponent)
      },
      {
        path: 'system',
        loadComponent: () => import('./system/admin-system.component').then(m => m.AdminSystemComponent)
      }
    ]
  }
];
