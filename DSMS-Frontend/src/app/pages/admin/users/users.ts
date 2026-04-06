import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './users.html',
  styleUrl: './users.scss'
})
export class UsersPage implements OnInit {
  users: any[] = [];
  roles: any[] = [];
  branches: any[] = [];
  isLoading = true;
  showForm = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  private apiUrl = 'http://localhost:5062/api';

  get isCompanyAdmin() { return this.auth.isCompanyAdmin(); }

  form = { userName: '', password: '', userFullName: '', roleId: 0, branchId: null as number | null };

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() { this.loadUsers(); this.loadRoles(); this.loadBranches(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadUsers() {
    this.http.get(`${this.apiUrl}/users`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.users = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  loadRoles() {
    this.http.get(`${this.apiUrl}/users/roles`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.roles = data; }
    });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  openForm() { this.showForm = true; this.form = { userName: '', password: '', userFullName: '', roleId: 0, branchId: null }; }
  cancel() { this.showForm = false; }

  save() {
    this.isSaving = true;
    this.errorMessage = '';
    this.http.post(`${this.apiUrl}/users`, this.form, { headers: this.getHeaders() }).subscribe({
      next: () => { this.isSaving = false; this.showForm = false; this.successMessage = 'User created!'; this.loadUsers(); setTimeout(() => { this.successMessage = ''; }, 3000); },
      error: (err: any) => { this.isSaving = false; this.errorMessage = err.error?.message || 'Error'; }
    });
  }

  resetPassword(id: number) {
    if (!confirm('Reset this user\'s password to Welcome@1234?')) return;
    this.http.post(`${this.apiUrl}/users/${id}/reset-password`, {}, { headers: this.getHeaders() }).subscribe({
      next: () => { this.successMessage = 'Password reset to Welcome@1234'; setTimeout(() => { this.successMessage = ''; }, 4000); },
      error: () => {}
    });
  }
}
