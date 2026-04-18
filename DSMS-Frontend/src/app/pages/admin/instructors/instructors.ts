import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-instructors',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './instructors.html',
  styleUrl: './instructors.scss'
})
export class InstructorsPage implements OnInit {
  private apiUrl = 'http://localhost:5062/api';

  instructors: any[] = [];
  filtered:    any[] = [];
  isLoading    = true;
  searchTerm   = '';

  // users with Instructor role — for linking
  instructorUsers: any[] = [];

  // branches — for Company Admin
  branches: any[] = [];

  // modal
  showModal  = false;
  isEditing  = false;
  isSaving   = false;
  saveError  = '';
  saveSuccess = '';

  form = {
    id: 0, instructorName: '', nic: '', phone: '',
    email: '', licenseNo: '', branchId: 0, userId: null as number | null, active: true
  };

  // delete confirm
  deletingId   = 0;
  deletingName = '';

  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }

  constructor(private authService: AuthService, private http: HttpClient) {}

  ngOnInit() {
    this.loadInstructors();
    this.loadInstructorUsers();
    if (this.isCompanyAdmin) this.loadBranches();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadInstructors() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/instructors`, { headers: this.getHeaders() }).subscribe({
      next: (d) => { this.instructors = d; this.filtered = d; this.isLoading = false; },
      error: ()  => { this.isLoading = false; }
    });
  }

  loadInstructorUsers() {
    this.http.get<any[]>(`${this.apiUrl}/users`, { headers: this.getHeaders() }).subscribe({
      next: (users) => {
        this.instructorUsers = users.filter(u => u.roleName === 'Instructor');
      }
    });
  }

  loadBranches() {
    this.http.get<any[]>(`${this.apiUrl}/branch`, { headers: this.getHeaders() }).subscribe({
      next: (d) => { this.branches = d; }
    });
  }

  search() {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) { this.filtered = this.instructors; return; }
    this.filtered = this.instructors.filter(i =>
      i.instructorName?.toLowerCase().includes(term) ||
      i.nic?.toLowerCase().includes(term) ||
      i.phone?.includes(term)
    );
  }

  openCreate() {
    this.isEditing  = false;
    this.saveError  = '';
    this.saveSuccess = '';
    this.form = { id: 0, instructorName: '', nic: '', phone: '', email: '', licenseNo: '', branchId: 0, userId: null, active: true };
    this.showModal  = true;
  }

  openEdit(i: any) {
    this.isEditing  = true;
    this.saveError  = '';
    this.saveSuccess = '';
    this.form = {
      id: i.id, instructorName: i.instructorName, nic: i.nic,
      phone: i.phone, email: i.email ?? '', licenseNo: i.licenseNo ?? '',
      branchId: i.branchId, userId: i.userId ?? null, active: i.active ?? true
    };
    this.showModal = true;
  }

  closeModal() { this.showModal = false; }

  save() {
    if (!this.form.instructorName) { this.saveError = 'Name is required.'; return; }
    if (!this.form.nic)            { this.saveError = 'NIC is required.'; return; }
    if (!this.form.phone)          { this.saveError = 'Phone is required.'; return; }

    this.isSaving  = true;
    this.saveError = '';

    if (this.isEditing) {
      const payload = {
        instructorName: this.form.instructorName,
        phone:          this.form.phone,
        email:          this.form.email || null,
        licenseNo:      this.form.licenseNo || null,
        active:         this.form.active,
        userId:         this.form.userId || null
      };
      this.http.put(`${this.apiUrl}/instructors/${this.form.id}`, payload, { headers: this.getHeaders() }).subscribe({
        next: () => { this.isSaving = false; this.saveSuccess = 'Instructor updated!'; this.loadInstructors(); setTimeout(() => this.closeModal(), 1100); },
        error: (err) => { this.isSaving = false; this.saveError = err.error?.message || 'Failed to save.'; }
      });
    } else {
      const payload = {
        instructorName: this.form.instructorName,
        nic:            this.form.nic,
        phone:          this.form.phone,
        email:          this.form.email || null,
        licenseNo:      this.form.licenseNo || null,
        branchId:       this.form.branchId,
        userId:         this.form.userId || null
      };
      this.http.post(`${this.apiUrl}/instructors`, payload, { headers: this.getHeaders() }).subscribe({
        next: () => { this.isSaving = false; this.saveSuccess = 'Instructor added!'; this.loadInstructors(); setTimeout(() => this.closeModal(), 1100); },
        error: (err) => { this.isSaving = false; this.saveError = err.error?.message || 'Failed to save.'; }
      });
    }
  }

  confirmDelete(i: any) { this.deletingId = i.id; this.deletingName = i.instructorName; }
  cancelDelete()        { this.deletingId = 0; this.deletingName = ''; }

  doDelete() {
    this.http.delete(`${this.apiUrl}/instructors/${this.deletingId}`, { headers: this.getHeaders() }).subscribe({
      next: () => { this.cancelDelete(); this.loadInstructors(); },
      error: () => { this.cancelDelete(); }
    });
  }

  getUserName(userId: number | null): string {
    if (!userId) return '—';
    return this.instructorUsers.find(u => u.id === userId)?.userFullName ?? '—';
  }
}
