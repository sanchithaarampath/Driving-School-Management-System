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
  stats: any = {};
  recentStudents: any[] = [];
  recentPayments: any[] = [];
  branches: any[] = [];
  pendingPractical: any[] = [];
  private apiUrl = 'http://localhost:5062/api';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.authService.getUser();

    // Redirect instructor to their own dashboard
    if (this.authService.isInstructor()) {
      this.router.navigate(['/instructor/dashboard']);
      return;
    }

    this.loadDashboard();
    if (this.authService.isCompanyAdmin()) this.loadBranches();
    if (!this.authService.isStaff()) this.loadPendingPractical();
  }

  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }
  get isBranchAdmin()  { return this.authService.isBranchAdmin(); }
  get isStaff()        { return this.authService.isStaff(); }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadDashboard() {
    this.http.get(`${this.apiUrl}/dashboard/stats`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.stats = data; }, error: () => {}
    });
    this.http.get(`${this.apiUrl}/dashboard/recent-students`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentStudents = data; }
    });
    this.http.get(`${this.apiUrl}/dashboard/recent-payments`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentPayments = data; }
    });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/branch`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  loadPendingPractical() {
    this.http.get(`${this.apiUrl}/exam/pending-practical`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.pendingPractical = data; }, error: () => {}
    });
  }

  addStudent()    { this.router.navigate(['/students/new']); }
  goToStudents()  { this.router.navigate(['/students']); }
  goToBilling()   { this.router.navigate(['/billing']); }
  goToExam()      { this.router.navigate(['/exam']); }
  goToBranches()  { this.router.navigate(['/branches']); }
  goToUsers()     { this.router.navigate(['/users']); }
}
