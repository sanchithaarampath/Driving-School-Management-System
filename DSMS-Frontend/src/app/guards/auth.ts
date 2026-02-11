import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  if (!token) { router.navigate(['/login']); return false; }

  // Role-based route restriction
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const role: string = user.role ?? '';
  const path = route.routeConfig?.path ?? '';

  // Instructor-only routes
  if (path.startsWith('instructor') && role !== 'Instructor') {
    router.navigate(['/dashboard']); return false;
  }
  // Instructors can view students but not create/edit
  if ((path === 'students/new' || path === 'students/edit/:id') && role === 'Instructor') {
    router.navigate(['/students']); return false;
  }
  // Staff cannot access admin-only areas
  if (path === 'branches' && role !== 'Company Admin') {
    router.navigate(['/dashboard']); return false;
  }
  if (path === 'users' && role === 'Staff') {
    router.navigate(['/dashboard']); return false;
  }

  return true;
};

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const router = inject(Router);
    const token = localStorage.getItem('token');
    if (!token) { router.navigate(['/login']); return false; }
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const effectiveRole = (user.role === 'Admin') ? 'Company Admin' : user.role;
    if (allowedRoles.includes(effectiveRole)) return true;
    router.navigate(['/dashboard']);
    return false;
  };
};
