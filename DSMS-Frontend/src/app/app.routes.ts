import { Routes } from '@angular/router';
import { authGuard } from './guards/auth';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { 
    path: 'login', 
    loadComponent: () => import('./pages/login/login').then(m => m.Login)
  },
  { 
    path: 'dashboard', 
    loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard),
    canActivate: [authGuard]
  },
  { 
    path: 'students', 
    loadComponent: () => import('./pages/students/student-list/student-list').then(m => m.StudentList),
    canActivate: [authGuard]
  },
  { 
    path: 'students/new', 
    loadComponent: () => import('./pages/students/student-form/student-form').then(m => m.StudentForm),
    canActivate: [authGuard]
  },
  { 
    path: 'students/edit/:id', 
    loadComponent: () => import('./pages/students/student-form/student-form').then(m => m.StudentForm),
    canActivate: [authGuard]
  },
  { 
    path: 'billing', 
    loadComponent: () => import('./pages/billing/billing-list/billing-list').then(m => m.BillingList),
    canActivate: [authGuard]
  },
  { 
    path: 'billing/new', 
    loadComponent: () => import('./pages/billing/billing-form/billing-form').then(m => m.BillingForm),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/login' }
];
