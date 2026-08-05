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
    loadComponent: () => import('./shell/admin-shell.component').then(m => m.AdminShellComponent)
  }
];
