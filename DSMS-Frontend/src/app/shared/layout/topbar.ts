import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-topbar',
  standalone: true,
  encapsulation: ViewEncapsulation.None,
  imports: [CommonModule],
  template: `
    <div class="topbar">
      <div class="topbar-left"><h5>Driving School Management System</h5></div>
      <div class="topbar-right">
        <div class="user-info">
          <div class="user-avatar"><i class="bi bi-person-fill"></i></div>
          <div class="user-details">
            <span class="user-name">{{ user?.userFullName }}</span>
            <span class="user-role">{{ user?.role }}</span>
          </div>
        </div>
        <button class="btn btn-logout" (click)="logout()">
          <i class="bi bi-box-arrow-right"></i> Logout
        </button>
      </div>
    </div>
  `
})
export class TopbarComponent {
  constructor(private auth: AuthService) {}
  get user() { return this.auth.getUser(); }
  logout() { this.auth.logout(); }
}
