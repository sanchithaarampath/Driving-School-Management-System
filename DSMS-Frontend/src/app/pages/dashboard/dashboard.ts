import { Component, OnInit } from '@angular/core';
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
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadDashboard() {
    this.http.get(`${this.apiUrl}/dashboard/stats`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.stats = data; },
      error: () => {}
    });
    this.http.get(`${this.apiUrl}/dashboard/recent-students`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentStudents = data; }
    });
    this.http.get(`${this.apiUrl}/dashboard/recent-payments`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.recentPayments = data; }
    });
  }

  logout() { this.authService.logout(); }
  goToStudents() { this.router.navigate(['/students']); }
  goToBilling() { this.router.navigate(['/billing']); }
  addStudent() { this.router.navigate(['/students/new']); }
}