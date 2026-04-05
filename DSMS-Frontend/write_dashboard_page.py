import os

dashboard_ts = '''import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  user: any;
  stats = { totalStudents: 0, totalBills: 0, totalIncome: 0, pendingAmount: 0, pendingBills: 0 };
  recentStudents: any[] = [];
  recentPayments: any[] = [];
  private apiUrl = 'http://localhost:5062/api';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadDashboard();
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: Bearer  });
  }

  loadDashboard() {
    this.http.get(${this.apiUrl}/dashboard/stats, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.stats = data; },
      error: () => {}
    });
    this.http.get(${this.apiUrl}/dashboard/recent-students, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentStudents = data; }
    });
    this.http.get(${this.apiUrl}/dashboard/recent-payments, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentPayments = data; }
    });
  }

  logout() { this.authService.logout(); }
  goToStudents() { this.router.navigate(['/students']); }
  goToBilling() { this.router.navigate(['/billing']); }
  addStudent() { this.router.navigate(['/students/new']); }
}
'''

dashboard_html = '''<div class="dsms-layout">
  <div class="sidebar">
    <div class="sidebar-brand">
      <div class="brand-logo"><i class="bi bi-car-front-fill"></i></div>
      <div class="brand-text">
        <span class="brand-name">Arampath</span>
        <span class="brand-sub">Driving School</span>
      </div>
    </div>
    <nav class="sidebar-nav">
      <a class="nav-item active" routerLink="/dashboard"><i class="bi bi-speedometer2"></i><span>Dashboard</span></a>
      <a class="nav-item" routerLink="/students"><i class="bi bi-people"></i><span>Students</span></a>
      <a class="nav-item" routerLink="/billing"><i class="bi bi-receipt"></i><span>Billing</span></a>
    </nav>
    <div class="sidebar-footer"><span>DSMS v1</span></div>
  </div>
  <div class="main-content">
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
        <button class="btn btn-logout" (click)="logout()"><i class="bi bi-box-arrow-right"></i> Logout</button>
      </div>
    </div>
    <div class="page-content">
      <div class="page-header">
        <h2>Dashboard</h2>
        <p>Quick access to main modules</p>
      </div>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon students"><i class="bi bi-people-fill"></i></div>
          <div class="stat-info">
            <span class="stat-value">{{ stats.totalStudents }}</span>
            <span class="stat-label">Total Students</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon income"><i class="bi bi-cash-coin"></i></div>
          <div class="stat-info">
            <span class="stat-value">Rs. {{ stats.totalIncome | number }}</span>
            <span class="stat-label">Total Income</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon pending"><i class="bi bi-clock-history"></i></div>
          <div class="stat-info">
            <span class="stat-value">Rs. {{ stats.pendingAmount | number }}</span>
            <span class="stat-label">Pending Payments</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon bills"><i class="bi bi-receipt"></i></div>
          <div class="stat-info">
            <span class="stat-value">{{ stats.totalBills }}</span>
            <span class="stat-label">Total Bills</span>
          </div>
        </div>
      </div>
      <div class="quick-actions">
        <div class="action-card" (click)="addStudent()">
          <div class="action-icon add"><i class="bi bi-plus-lg"></i></div>
          <h6>Add New Student</h6>
          <p>Register a new student and create a profile.</p>
          <button class="btn btn-primary">Add Student</button>
        </div>
        <div class="action-card" (click)="goToStudents()">
          <div class="action-icon students"><i class="bi bi-people"></i></div>
          <h6>Existing Students</h6>
          <p>Search, view and update student details.</p>
          <button class="btn btn-outline">View Students</button>
        </div>
        <div class="action-card" (click)="goToBilling()">
          <div class="action-icon billing"><i class="bi bi-currency-rupee"></i></div>
          <h6>Billing / Payments</h6>
          <p>Create bills, record payments and view history.</p>
          <button class="btn btn-outline">Go to Billing</button>
        </div>
      </div>
      <div class="recent-grid">
        <div class="recent-card">
          <div class="recent-header">
            <h6><i class="bi bi-people me-2"></i>Recent Students</h6>
            <a routerLink="/students" class="view-all">View All</a>
          </div>
          <div class="recent-body">
            <div *ngIf="recentStudents.length === 0" class="empty-state">No students yet</div>
            <div *ngFor="let s of recentStudents" class="recent-item">
              <div class="item-avatar"><i class="bi bi-person-circle"></i></div>
              <div class="item-info">
                <span class="item-name">{{ s.studentName }}</span>
                <span class="item-sub">{{ s.nic }} | {{ s.branchName }}</span>
              </div>
              <span class="item-date">{{ s.registrationDate | date:"dd/MM/yy" }}</span>
            </div>
          </div>
        </div>
        <div class="recent-card">
          <div class="recent-header">
            <h6><i class="bi bi-cash me-2"></i>Recent Payments</h6>
            <a routerLink="/billing" class="view-all">View All</a>
          </div>
          <div class="recent-body">
            <div *ngIf="recentPayments.length === 0" class="empty-state">No payments yet</div>
            <div *ngFor="let p of recentPayments" class="recent-item">
              <div class="item-avatar payment"><i class="bi bi-cash-coin"></i></div>
              <div class="item-info">
                <span class="item-name">{{ p.studentName }}</span>
                <span class="item-sub">{{ p.paymentMethod }}</span>
              </div>
              <span class="item-amount">Rs. {{ p.amount | number }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
'''

dashboard_scss = '''.dsms-layout { display: flex; min-height: 100vh; background: #0d1117; }
.sidebar { width: 240px; min-width: 240px; background: #010409; border-right: 1px solid #30363d; display: flex; flex-direction: column; padding: 1.5rem 0; }
.sidebar-brand { display: flex; align-items: center; gap: 0.75rem; padding: 0 1.25rem 1.5rem; border-bottom: 1px solid #30363d; margin-bottom: 1rem; }
.brand-logo { width: 42px; height: 42px; background: linear-gradient(135deg, #e63946, #c1121f); border-radius: 10px; display: flex; align-items: center; justify-content: center; font-size: 1.2rem; color: white; }
.brand-name { color: #e6edf3; font-weight: 700; font-size: 0.95rem; display: block; }
.brand-sub { color: #8b949e; font-size: 0.75rem; display: block; }
.sidebar-nav { flex: 1; padding: 0 0.75rem; }
.nav-item { display: flex; align-items: center; gap: 0.75rem; padding: 0.7rem 0.75rem; border-radius: 8px; color: #8b949e; text-decoration: none; margin-bottom: 0.25rem; transition: all 0.2s; font-size: 0.9rem; }
.nav-item i { font-size: 1.1rem; }
.nav-item:hover { background: #161b22; color: #e6edf3; }
.nav-item.active { background: rgba(230,57,70,0.15); color: #e63946; font-weight: 600; }
.sidebar-footer { padding: 1rem 1.25rem 0; color: #8b949e; font-size: 0.75rem; border-top: 1px solid #30363d; }
.main-content { flex: 1; display: flex; flex-direction: column; }
.topbar { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.5rem; background: #0d1117; border-bottom: 1px solid #30363d; }
.topbar h5 { color: #e6edf3; font-weight: 700; margin: 0; font-size: 1rem; }
.topbar-right { display: flex; align-items: center; gap: 1rem; }
.user-info { display: flex; align-items: center; gap: 0.5rem; }
.user-avatar { width: 36px; height: 36px; background: #30363d; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #8b949e; }
.user-name { color: #e6edf3; font-size: 0.85rem; font-weight: 600; display: block; }
.user-role { color: #8b949e; font-size: 0.75rem; display: block; }
.btn-logout { background: transparent; border: 1px solid #30363d; color: #8b949e; padding: 0.4rem 0.75rem; border-radius: 6px; font-size: 0.85rem; cursor: pointer; }
.btn-logout:hover { border-color: #e63946; color: #e63946; }
.page-content { flex: 1; padding: 1.5rem; overflow-y: auto; }
.page-header { margin-bottom: 1.5rem; }
.page-header h2 { color: #e6edf3; font-size: 1.5rem; font-weight: 700; margin: 0; }
.page-header p { color: #8b949e; font-size: 0.85rem; margin: 0; }
.stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
.stat-card { background: #161b22; border: 1px solid #30363d; border-radius: 10px; padding: 1.25rem; display: flex; align-items: center; gap: 1rem; }
.stat-icon { width: 48px; height: 48px; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-size: 1.3rem; }
.stat-icon.students { background: rgba(88,166,255,0.15); color: #58a6ff; }
.stat-icon.income { background: rgba(35,134,54,0.15); color: #3fb950; }
.stat-icon.pending { background: rgba(210,153,34,0.15); color: #d29922; }
.stat-icon.bills { background: rgba(230,57,70,0.15); color: #e63946; }
.stat-value { color: #e6edf3; font-size: 1.3rem; font-weight: 700; display: block; }
.stat-label { color: #8b949e; font-size: 0.8rem; display: block; }
.quick-actions { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
.action-card { background: #161b22; border: 1px solid #30363d; border-radius: 10px; padding: 1.5rem; cursor: pointer; transition: border-color 0.2s; }
.action-card:hover { border-color: #e63946; }
.action-card h6 { color: #e6edf3; font-weight: 600; margin-bottom: 0.5rem; }
.action-card p { color: #8b949e; font-size: 0.85rem; margin-bottom: 1rem; }
.action-icon { width: 44px; height: 44px; border-radius: 10px; margin-bottom: 1rem; display: flex; align-items: center; justify-content: center; font-size: 1.2rem; }
.action-icon.add { background: rgba(230,57,70,0.15); color: #e63946; }
.action-icon.students { background: rgba(88,166,255,0.15); color: #58a6ff; }
.action-icon.billing { background: rgba(35,134,54,0.15); color: #3fb950; }
.btn-outline { background: transparent; border: 1px solid #30363d; color: #e6edf3; padding: 0.5rem 1rem; border-radius: 6px; font-size: 0.85rem; cursor: pointer; }
.recent-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.recent-card { background: #161b22; border: 1px solid #30363d; border-radius: 10px; overflow: hidden; }
.recent-header { display: flex; align-items: center; justify-content: space-between; padding: 1rem 1.25rem; border-bottom: 1px solid #30363d; }
.recent-header h6 { color: #e6edf3; margin: 0; font-size: 0.9rem; }
.view-all { color: #e63946; font-size: 0.8rem; text-decoration: none; }
.recent-body { padding: 0.5rem 0; }
.empty-state { color: #8b949e; text-align: center; padding: 1.5rem; font-size: 0.85rem; }
.recent-item { display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem 1.25rem; }
.recent-item:hover { background: rgba(255,255,255,0.03); }
.item-avatar { width: 36px; height: 36px; background: #30363d; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #8b949e; }
.item-avatar.payment { background: rgba(35,134,54,0.2); color: #3fb950; }
.item-info { flex: 1; }
.item-name { color: #e6edf3; font-size: 0.85rem; font-weight: 500; display: block; }
.item-sub { color: #8b949e; font-size: 0.75rem; display: block; }
.item-date { color: #8b949e; font-size: 0.75rem; }
.item-amount { color: #3fb950; font-size: 0.85rem; font-weight: 600; }
'''

os.makedirs("src/app/pages/dashboard", exist_ok=True)
with open("src/app/pages/dashboard/dashboard.ts", "w", encoding="utf-8") as f:
    f.write(dashboard_ts)
with open("src/app/pages/dashboard/dashboard.html", "w", encoding="utf-8") as f:
    f.write(dashboard_html)
with open("src/app/pages/dashboard/dashboard.scss", "w", encoding="utf-8") as f:
    f.write(dashboard_scss)
print("Dashboard created successfully!")
