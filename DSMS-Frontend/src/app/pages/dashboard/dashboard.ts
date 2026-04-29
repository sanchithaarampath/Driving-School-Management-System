import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../services/auth';
import { SidebarComponent } from '../../shared/layout/sidebar';
import { TopbarComponent } from '../../shared/layout/topbar';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  user: any;
  stats: any        = {};
  recentStudents: any[] = [];
  recentPayments: any[] = [];
  branches: any[]       = [];
  pendingPractical: any[] = [];
  monthlyRevenue: any[] = [];
  packageBreakdown: any[] = [];

  private apiUrl = 'http://localhost:5062/api';

  // Palette for package breakdown bars
  pkgColors = ['#e63946','#3fb950','#79c0ff','#a0a0ff','#f0883e','#ffa657'];

  constructor(
    private authService: AuthService,
    public  router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    if (this.authService.isInstructor()) {
      this.router.navigate(['/instructor/dashboard']);
      return;
    }
    this.loadDashboard();
    if (this.authService.isCompanyAdmin()) this.loadBranches();
    if (!this.authService.isStaff())       this.loadPendingPractical();
  }

  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }
  get isBranchAdmin()  { return this.authService.isBranchAdmin(); }
  get isStaff()        { return this.authService.isStaff(); }

  get greeting() {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  get displayName(): string {
    return this.user?.userFullName || this.user?.userName || 'User';
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadDashboard() {
    const h = { headers: this.getHeaders() };
    this.http.get(`${this.apiUrl}/dashboard/stats`,             h).subscribe({ next: (d: any) => { this.stats = d; } });
    this.http.get(`${this.apiUrl}/dashboard/recent-students`,   h).subscribe({ next: (d: any) => { this.recentStudents = d; } });
    this.http.get(`${this.apiUrl}/dashboard/recent-payments`,   h).subscribe({ next: (d: any) => { this.recentPayments = d; } });
    this.http.get(`${this.apiUrl}/dashboard/monthly-revenue`,   h).subscribe({ next: (d: any) => { this.monthlyRevenue = d; } });
    this.http.get(`${this.apiUrl}/dashboard/package-breakdown`, h).subscribe({ next: (d: any) => { this.packageBreakdown = d; } });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/branch`, { headers: this.getHeaders() }).subscribe({
      next: (d: any) => { this.branches = d; }
    });
  }

  loadPendingPractical() {
    this.http.get(`${this.apiUrl}/exam/pending-practical`, { headers: this.getHeaders() }).subscribe({
      next: (d: any) => { this.pendingPractical = d; }, error: () => {}
    });
  }

  // ── Revenue Chart ─────────────────────────────────────────────────────────
  get chartBars() {
    const max = Math.max(...this.monthlyRevenue.map((m: any) => m.revenue), 1);
    return this.monthlyRevenue.map((m: any) => ({
      ...m,
      heightPct: Math.max(4, Math.round((m.revenue / max) * 100))
    }));
  }

  // ── Package Breakdown ─────────────────────────────────────────────────────
  get pkgTotal() {
    return this.packageBreakdown.reduce((s: number, p: any) => s + p.count, 0) || 1;
  }

  pkgWidthPct(count: number): number {
    return Math.max(4, Math.round((count / this.pkgTotal) * 100));
  }

  // ── 4th KPI card action (context-dependent) ───────────────────────────────
  kpiExtraClick() {
    if (this.isCompanyAdmin) this.goToBranches();
    else if (this.isBranchAdmin) this.goToExam();
    else this.goToBilling();
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  addStudent()   { this.router.navigate(['/students/new']); }
  goToStudents() { this.router.navigate(['/students']); }
  goToBilling()  { this.router.navigate(['/billing']); }
  goToExam()     { this.router.navigate(['/exam']); }
  goToBranches() { this.router.navigate(['/branches']); }
  goToUsers()    { this.router.navigate(['/users']); }
}
