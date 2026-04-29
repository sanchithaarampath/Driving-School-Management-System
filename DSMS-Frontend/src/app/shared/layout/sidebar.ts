import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  encapsulation: ViewEncapsulation.None,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="sidebar">
      <div class="sidebar-brand">
        <div class="brand-logo"><i class="bi bi-car-front-fill"></i></div>
        <div class="brand-text">
          <span class="brand-name">Arampath</span>
          <span class="brand-sub">Driving School</span>
        </div>
      </div>
      <nav class="sidebar-nav">
        <a class="nav-item" routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">
          <i class="bi bi-speedometer2"></i><span>Dashboard</span>
        </a>
        <a class="nav-item" routerLink="/students" routerLinkActive="active">
          <i class="bi bi-people"></i><span>Students</span>
        </a>
        <ng-container *ngIf="!isInstructor">
          <a class="nav-item" routerLink="/billing" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">
            <i class="bi bi-receipt"></i><span>Billing</span>
          </a>
          <a class="nav-item nav-sub" routerLink="/billing/pending" routerLinkActive="active">
            <i class="bi bi-clock-history"></i><span>Pending Payments</span>
          </a>
        </ng-container>
        <ng-container *ngIf="isBranchAdmin || isCompanyAdmin">
          <a class="nav-item" routerLink="/exam" routerLinkActive="active">
            <i class="bi bi-clipboard-check"></i><span>Exam Results</span>
          </a>
          <a class="nav-item" routerLink="/employees" routerLinkActive="active">
            <i class="bi bi-person-badge"></i><span>Employees</span>
          </a>
          <a class="nav-item" routerLink="/users" routerLinkActive="active">
            <i class="bi bi-shield-person"></i><span>Users</span>
          </a>
          <a class="nav-item" routerLink="/admin/course-packages" routerLinkActive="active">
            <i class="bi bi-box-seam"></i><span>Course Packages</span>
          </a>
        </ng-container>
        <ng-container *ngIf="isCompanyAdmin">
          <a class="nav-item" routerLink="/branches" routerLinkActive="active">
            <i class="bi bi-building"></i><span>Branches</span>
          </a>
        </ng-container>
        <ng-container *ngIf="isInstructor">
          <a class="nav-item" routerLink="/instructor/dashboard" routerLinkActive="active">
            <i class="bi bi-speedometer2"></i><span>My Dashboard</span>
          </a>
          <a class="nav-item" routerLink="/instructor/attendance" routerLinkActive="active">
            <i class="bi bi-calendar-check"></i><span>Attendance</span>
          </a>
        </ng-container>
      </nav>
      <div class="sidebar-footer"><span>DSMS v2</span></div>
    </div>
  `
})
export class SidebarComponent {
  constructor(private auth: AuthService, private router: Router) {}
  get isCompanyAdmin() { return this.auth.isCompanyAdmin(); }
  get isBranchAdmin() { return this.auth.isBranchAdmin(); }
  get isStaff() { return this.auth.isStaff(); }
  get isInstructor() { return this.auth.isInstructor(); }
}
