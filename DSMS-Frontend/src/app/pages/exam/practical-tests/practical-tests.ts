import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-practical-tests',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './practical-tests.html',
  styleUrl: './practical-tests.scss'
})
export class PracticalTestsPage implements OnInit {

  // Search
  searchTerm   = '';
  searchResult: any[] = [];
  isSearching  = false;

  // Selected student & their attempts
  selectedStudent: any = null;
  attempts: any[] = [];
  isLoadingAttempts = false;

  // Record form
  form = {
    studentId:     0,
    testDate:      new Date().toISOString().split('T')[0],
    result:        '',
    remarks:       '',
    licenseNumber: ''
  };

  isSaving       = false;
  successMessage = '';
  errorMessage   = '';
  deleteConfirmId: number | null = null;

  // License panel
  licenseForm = { issuedLicenseNo: '', licenseIssuedDate: new Date().toISOString().split('T')[0] };
  isSavingLicense = false;
  showLicenseForm = false;

  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() {}

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  searchStudents() {
    if (!this.searchTerm.trim()) return;
    this.isSearching  = true;
    this.errorMessage = '';
    this.http.get<any[]>(`${this.apiUrl}/student/search?q=${encodeURIComponent(this.searchTerm)}`,
      { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.searchResult = data; this.isSearching = false; },
      error: ()    => { this.isSearching = false; }
    });
  }

  selectStudent(s: any) {
    this.selectedStudent  = s;
    this.searchResult     = [];
    this.successMessage   = '';
    this.errorMessage     = '';
    this.showLicenseForm  = false;
    this.form = {
      studentId:     s.id,
      testDate:      new Date().toISOString().split('T')[0],
      result:        '',
      remarks:       '',
      licenseNumber: ''
    };
    this.loadAttempts(s.id);
    this.loadStudentDetail(s.id);
  }

  loadStudentDetail(id: number) {
    this.http.get<any>(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.selectedStudent = { ...this.selectedStudent, ...data };
        this.licenseForm.issuedLicenseNo   = data.issuedLicenseNo  ?? '';
        this.licenseForm.licenseIssuedDate = data.licenseIssuedDate
          ? new Date(data.licenseIssuedDate).toISOString().split('T')[0]
          : new Date().toISOString().split('T')[0];
      }
    });
  }

  loadAttempts(studentId: number) {
    this.isLoadingAttempts = true;
    this.http.get<any[]>(`${this.apiUrl}/practical-test/student/${studentId}`,
      { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.attempts = data; this.isLoadingAttempts = false; },
      error: ()    => { this.isLoadingAttempts = false; }
    });
  }

  recordAttempt() {
    if (!this.form.result) { this.errorMessage = 'Please select a result (Pass or Fail).'; return; }
    this.isSaving     = true;
    this.errorMessage = '';
    this.http.post(`${this.apiUrl}/practical-test/attempt`, this.form, { headers: this.getHeaders() }).subscribe({
      next: (res: any) => {
        this.isSaving       = false;
        this.successMessage = res.message || 'Attempt recorded successfully.';
        this.form.result    = '';
        this.form.remarks   = '';
        this.form.licenseNumber = '';
        this.loadAttempts(this.form.studentId);
        this.loadStudentDetail(this.form.studentId);
        setTimeout(() => { this.successMessage = ''; }, 4000);
      },
      error: (err: any) => {
        this.isSaving     = false;
        this.errorMessage = err.error?.message || 'Failed to record attempt.';
      }
    });
  }

  requestDelete(id: number)  { this.deleteConfirmId = id; }
  cancelDelete()             { this.deleteConfirmId = null; }

  confirmDelete() {
    if (!this.deleteConfirmId) return;
    const id = this.deleteConfirmId;
    this.deleteConfirmId = null;
    this.http.delete(`${this.apiUrl}/practical-test/attempt/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => { this.loadAttempts(this.form.studentId); }
    });
  }

  saveLicense() {
    this.isSavingLicense = true;
    this.errorMessage    = '';
    this.http.patch(`${this.apiUrl}/student/${this.selectedStudent.id}/license`,
      this.licenseForm, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSavingLicense = false;
        this.successMessage  = 'License number saved successfully.';
        this.showLicenseForm = false;
        this.loadStudentDetail(this.selectedStudent.id);
        setTimeout(() => { this.successMessage = ''; }, 4000);
      },
      error: (err: any) => {
        this.isSavingLicense = false;
        this.errorMessage    = err.error?.message || 'Failed to save license.';
      }
    });
  }

  get passCount() { return this.attempts.filter(a => a.result === 'Pass').length; }
  get failCount() { return this.attempts.filter(a => a.result === 'Fail').length; }
  get latestResult() { return this.attempts.length ? this.attempts[0].result : null; }
}
