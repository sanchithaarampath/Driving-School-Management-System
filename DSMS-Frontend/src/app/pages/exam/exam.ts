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
  isLoading = true;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  searchTerm = '';
  searchResult: any = null;
  private apiUrl = 'http://localhost:5062/api';

  examForm = { studentPackageRegistrationId: 0, examStatus: '', examDate: '' };

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadPendingPractical();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadPendingPractical() {
    this.http.get(`${this.apiUrl}/exam/pending-practical`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.pendingPractical = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  searchStudent() {
    if (!this.searchTerm) return;
    this.http.get(`${this.apiUrl}/student/search?q=${this.searchTerm}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.searchResult = data; }
    });
  }

  loadStudentExams(studentId: number) {
    this.http.get(`${this.apiUrl}/exam/student/${studentId}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        if (data.length > 0) {
          this.examForm.studentPackageRegistrationId = data[0].id;
        }
      }
    });
  }

  updateExamResult() {
    if (!this.examForm.studentPackageRegistrationId || !this.examForm.examStatus) {
      this.errorMessage = 'Please fill all required fields';
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';
    this.http.put(`${this.apiUrl}/exam/update`, this.examForm, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = `Exam result updated: ${this.examForm.examStatus}`;
        this.examForm = { studentPackageRegistrationId: 0, examStatus: '', examDate: '' };
        this.loadPendingPractical();
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: (err: any) => { this.isSaving = false; this.errorMessage = err.error?.message || 'Error'; }
    });
  }
}
