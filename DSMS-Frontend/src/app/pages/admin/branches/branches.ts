import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-branches',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './branches.html',
  styleUrl: './branches.scss'
})
export class BranchesPage implements OnInit {
  branches: any[] = [];
  isLoading = true;
  showForm = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  editId: number | null = null;
  private apiUrl = 'http://localhost:5062/api';

  form = { name: '', code: '', address: '', phone: '' };

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() { this.loadBranches(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadBranches() {
    this.http.get(`${this.apiUrl}/branch`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  openForm(branch?: any) {
    this.showForm = true;
    this.editId = branch ? branch.id : null;
    this.form = branch ? { name: branch.name, code: branch.code, address: branch.address, phone: branch.phone } : { name: '', code: '', address: '', phone: '' };
  }

  save() {
    this.isSaving = true;
    this.errorMessage = '';
    const req = this.editId
      ? this.http.put(`${this.apiUrl}/branch/${this.editId}`, this.form, { headers: this.getHeaders() })
      : this.http.post(`${this.apiUrl}/branch`, this.form, { headers: this.getHeaders() });
    req.subscribe({
      next: () => { this.isSaving = false; this.showForm = false; this.successMessage = 'Branch saved!'; this.loadBranches(); setTimeout(() => { this.successMessage = ''; }, 3000); },
      error: (err: any) => { this.isSaving = false; this.errorMessage = err.error?.message || 'Error'; }
    });
  }

  cancel() { this.showForm = false; this.editId = null; }
}
