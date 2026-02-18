import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, TopbarComponent],
  templateUrl: './student-list.html',
  styleUrl: './student-list.scss'
})
export class StudentList implements OnInit {
  students: any[]         = [];
  filteredStudents: any[] = [];
  branches: any[]         = [];
  searchTerm     = '';
  selectedBranch = 0;       // 0 = All Branches
  isLoading      = false;
  isSearching    = false;
  user: any;

  // Inline delete confirm
  deleteConfirmId: number | null = null;

  private apiUrl = 'http://localhost:5062/api';

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadStudents();
    if (this.isCompanyAdmin) this.loadBranches();
  }

  get isInstructor()   { return this.authService.isInstructor(); }
  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }
  get canEdit()        { return !this.authService.isInstructor(); }
  get canDelete()      { return this.authService.isCompanyAdmin() || this.authService.isBranchAdmin(); }
  get selectedBranchName(): string {
    if (!this.selectedBranch) return '';
    const b = this.branches.find(b => b.id === this.selectedBranch);
    return b ? b.name : '';
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadStudents() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.students = data;
        this.applyFilters();
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  loadBranches() {
    this.http.get<any[]>(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.branches = data; }
    });
  }

  // Called by both search input and branch dropdown
  search()           { this.applyFilters(); }
  onBranchChange()   { this.applyFilters(); }

  applyFilters() {
    const term = this.searchTerm.trim().toLowerCase();

    // Server-side search for bill numbers / long numeric IDs
    if (term && (term.toUpperCase().startsWith('BILL-') || /^\d{4,}$/.test(term))) {
      this.isSearching = true;
      this.http.get<any[]>(`${this.apiUrl}/student/search?q=${encodeURIComponent(term)}`, { headers: this.getHeaders() }).subscribe({
        next: (data) => {
          this.filteredStudents = this.selectedBranch
            ? data.filter(s => s.branchId === this.selectedBranch)
            : data;
          this.isSearching = false;
        },
        error: () => { this.isSearching = false; }
      });
      return;
    }

    // Local filter — text + branch
    this.filteredStudents = this.students.filter(s => {
      const matchesText = !term ||
        s.studentName?.toLowerCase().includes(term) ||
        s.nic?.toLowerCase().includes(term) ||
        s.phoneNumber?.toLowerCase().includes(term);

      const matchesBranch = !this.selectedBranch || s.branchId === this.selectedBranch;

      return matchesText && matchesBranch;
    });
  }

  clearSearch() {
    this.searchTerm = '';
    this.applyFilters();
  }

  clearBranch() {
    this.selectedBranch = 0;
    this.applyFilters();
  }

  // ── Delete (soft) ─────────────────────────────────────────────────────────
  requestDelete(id: number)  { this.deleteConfirmId = id; }
  cancelDelete()             { this.deleteConfirmId = null; }

  confirmDelete() {
    if (!this.deleteConfirmId) return;
    const id = this.deleteConfirmId;
    this.deleteConfirmId = null;
    this.http.delete(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.students         = this.students.filter(s => s.id !== id);
        this.filteredStudents = this.filteredStudents.filter(s => s.id !== id);
      }
    });
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  viewProfile(id: number) { this.router.navigate(['/students/profile', id]); }
  addStudent()            { this.router.navigate(['/students/new']); }
  editStudent(id: number) { this.router.navigate(['/students/edit', id]); }
}
