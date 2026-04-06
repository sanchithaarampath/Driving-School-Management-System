import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, TopbarComponent],
  template: `
    <div class="dsms-layout">
      <app-sidebar></app-sidebar>
      <div class="main-content">
        <app-topbar></app-topbar>
        <div class="page-content">
          <div class="page-header">
            <div><h2>Instructor Dashboard</h2><p>Welcome, {{ user?.userFullName }}</p></div>
            <button class="btn btn-primary" (click)="goToAttendance()">
              <i class="bi bi-calendar-check me-2"></i>Record Attendance
            </button>
          </div>
          <div class="stats-grid">
            <div class="stat-card primary">
              <div class="stat-icon"><i class="bi bi-people-fill"></i></div>
              <div class="stat-info"><span class="stat-value">{{ myStudents.length }}</span><span class="stat-label">My Students</span></div>
            </div>
            <div class="stat-card success">
              <div class="stat-icon"><i class="bi bi-check-circle"></i></div>
              <div class="stat-info"><span class="stat-value">{{ approvedCount }}</span><span class="stat-label">Approved for Practical</span></div>
            </div>
          </div>
          <div class="table-card">
            <div class="card-header"><h6>My Assigned Students</h6></div>
            <div *ngIf="isLoading" class="loading-state"><div class="spinner-border text-danger"></div></div>
            <table class="data-table" *ngIf="!isLoading && myStudents.length > 0">
              <thead><tr><th>Student</th><th>NIC</th><th>Training Days</th><th>Practical Approved</th><th>Action</th></tr></thead>
              <tbody>
                <tr *ngFor="let s of myStudents">
                  <td>{{ s.studentName }}</td>
                  <td>{{ s.nic }}</td>
                  <td>{{ s.attendanceDays || 0 }} / 15</td>
                  <td><span class="badge" [class.bg-success]="s.isRecommendForTrial" [class.bg-secondary]="!s.isRecommendForTrial">{{ s.isRecommendForTrial ? 'Yes' : 'No' }}</span></td>
                  <td><button class="btn-action edit" (click)="goToAttendanceFor(s.sprId)"><i class="bi bi-calendar-plus"></i></button></td>
                </tr>
              </tbody>
            </table>
            <div class="empty-state" *ngIf="!isLoading && myStudents.length === 0"><i class="bi bi-people"></i><p>No students assigned yet</p></div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class InstructorDashboard implements OnInit {
  user: any;
  myStudents: any[] = [];
  isLoading = true;
  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient, private router: Router) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadMyStudents();
  }

  get approvedCount() { return this.myStudents.filter(s => s.isRecommendForTrial).length; }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadMyStudents() {
    this.http.get(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.myStudents = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  goToAttendance() { this.router.navigate(['/instructor/attendance']); }
  goToAttendanceFor(sprId: number) { this.router.navigate(['/instructor/attendance'], { queryParams: { spr: sprId } }); }
}
