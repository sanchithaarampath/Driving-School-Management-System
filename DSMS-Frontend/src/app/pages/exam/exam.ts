import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../services/auth';
import { SidebarComponent } from '../../shared/layout/sidebar';
import { TopbarComponent } from '../../shared/layout/topbar';

@Component({
  selector: 'app-exam',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './exam.html',
  styleUrl: './exam.scss'
})
export class ExamPage implements OnInit {
  user: any;
  pendingPractical: any[] = [];
  filteredPending: any[] = [];
  filterSearch = '';
  isLoading = true;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  // Student search
  searchTerm = '';
  searchResult: any[] = [];
  isSearching = false;

  // Selected student for update
  selectedStudent: any = null;
  studentSprs: any[] = [];
  examForm = { studentPackageRegistrationId: 0, examStatus: '', examDate: new Date().toISOString().split('T')[0] };

  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadPendingPractical();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadPendingPractical() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/exam/pending-practical`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.pendingPractical = data; this.filteredPending = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  filterPending() {
    const term = this.filterSearch.toLowerCase();
    if (!term) { this.filteredPending = this.pendingPractical; return; }
    this.filteredPending = this.pendingPractical.filter(s =>
      s.studentName?.toLowerCase().includes(term) ||
      s.nic?.toLowerCase().includes(term) ||
      s.branchName?.toLowerCase().includes(term)
    );
  }

  searchStudent() {
    if (!this.searchTerm.trim()) return;
    this.isSearching = true;
    this.http.get<any[]>(`${this.apiUrl}/student/search?q=${encodeURIComponent(this.searchTerm)}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.searchResult = data; this.isSearching = false; },
      error: () => { this.isSearching = false; }
    });
  }

  selectStudentForExam(student: any) {
    this.selectedStudent = student;
    this.examForm = { studentPackageRegistrationId: 0, examStatus: '', examDate: new Date().toISOString().split('T')[0] };
    this.searchResult = [];
    this.http.get<any[]>(`${this.apiUrl}/exam/student/${student.id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.studentSprs = data;
        if (data.length > 0) this.examForm.studentPackageRegistrationId = data[0].id;
      }
    });
  }

  // Quick update directly from the pending table
  quickUpdate(spr: any, status: string) {
    const payload = {
      studentPackageRegistrationId: spr.id,
      examStatus: status,
      examDate: new Date().toISOString().split('T')[0]
    };
    this.http.put(`${this.apiUrl}/exam/update`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.successMessage = `${spr.studentName} — marked as ${status}`;
        this.loadPendingPractical();
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: (err: any) => { this.errorMessage = err.error?.message || 'Update failed'; }
    });
  }

  updateExamResult() {
    if (!this.examForm.studentPackageRegistrationId || !this.examForm.examStatus) {
      this.errorMessage = 'Please select a student and result.';
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';
    this.http.put(`${this.apiUrl}/exam/update`, this.examForm, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = `Exam result saved: ${this.examForm.examStatus}`;
        this.selectedStudent = null;
        this.studentSprs = [];
        this.loadPendingPractical();
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: (err: any) => { this.isSaving = false; this.errorMessage = err.error?.message || 'Error'; }
    });
  }

  get passCount() { return this.pendingPractical.filter(s => s.examStatus === 'Pass').length; }
  get failCount() { return this.pendingPractical.filter(s => s.examStatus === 'Fail').length; }
  get pendingCount() { return this.pendingPractical.filter(s => !s.examStatus).length; }
}
