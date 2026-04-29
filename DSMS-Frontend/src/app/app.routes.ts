import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './guards/auth';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.Login)
  },

  // ===== SHARED DASHBOARD (role-aware) =====
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard),
    canActivate: [authGuard]
  },

  // ===== STUDENTS (Staff, Branch Admin, Company Admin) =====
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
    path: 'students/profile/:id',
    loadComponent: () => import('./pages/students/student-profile/student-profile').then(m => m.StudentProfile),
    canActivate: [authGuard]
  },

  // ===== BILLING =====
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
  {
    path: 'billing/pending',
    loadComponent: () => import('./pages/billing/pending-payments/pending-payments').then(m => m.PendingPayments),
    canActivate: [authGuard]
  },

  // ===== EMPLOYEES =====
  {
    path: 'employees',
    loadComponent: () => import('./pages/employees/employee-list/employee-list').then(m => m.EmployeeList),
    canActivate: [authGuard]
  },
  {
    path: 'employees/new',
    loadComponent: () => import('./pages/employees/employee-form/employee-form').then(m => m.EmployeeForm),
    canActivate: [authGuard]
  },
  {
    path: 'employees/edit/:id',
    loadComponent: () => import('./pages/employees/employee-form/employee-form').then(m => m.EmployeeForm),
    canActivate: [authGuard]
  },

  // ===== EXAM RESULTS (Branch Admin, Company Admin) =====
  {
    path: 'exam',
    loadComponent: () => import('./pages/exam/exam').then(m => m.ExamPage),
    canActivate: [roleGuard(['Company Admin', 'Branch Admin'])]
  },

  // ===== COURSE PACKAGES (Company Admin + Branch Admin) =====
  {
    path: 'admin/course-packages',
    loadComponent: () => import('./pages/admin/course-packages/course-packages').then(m => m.CoursePackagesPage),
    canActivate: [roleGuard(['Company Admin', 'Branch Admin'])]
  },

  // ===== BRANCH MANAGEMENT (Company Admin only) =====
  {
    path: 'branches',
    loadComponent: () => import('./pages/admin/branches/branches').then(m => m.BranchesPage),
    canActivate: [roleGuard(['Company Admin'])]
  },

  // ===== USER MANAGEMENT (Company Admin + Branch Admin) =====
  {
    path: 'users',
    loadComponent: () => import('./pages/admin/users/users').then(m => m.UsersPage),
    canActivate: [roleGuard(['Company Admin', 'Branch Admin'])]
  },

  // ===== INSTRUCTOR ROUTES =====
  {
    path: 'instructor/dashboard',
    loadComponent: () => import('./pages/instructor/dashboard/instructor-dashboard').then(m => m.InstructorDashboard),
    canActivate: [roleGuard(['Instructor'])]
  },
  {
    path: 'instructor/attendance',
    loadComponent: () => import('./pages/instructor/attendance/attendance').then(m => m.AttendancePage),
    canActivate: [roleGuard(['Instructor'])]
  },

  { path: '**', redirectTo: '/login' }
];
