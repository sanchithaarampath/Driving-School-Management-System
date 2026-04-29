import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './attendance.html',
  styleUrl: './attendance.scss'
})
export class AttendancePage implements OnInit {
  user: any;

  studentsProgress: any[] = [];
  filteredProgress: any[] = [];
  searchTerm = '';
  isLoadingStudents = true;

  selected: any = null;
  attendanceRecords: any[] = [];
  isLoadingRecords = false;

  form = {
    studentPackageRegistrationId: 0,
    instructorId: null as number | null,
    attendanceDate: new Date().toISOString().split('T')[0],
    dayNumber: 1,
    notes: '',
    isReadyForPracticalTest: false
  };

  isSaving = false;
  successMessage = '';
  errorMessage   = '';
  deleteConfirmId: number | null = null;

  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient, private route: ActivatedRoute) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadStudentsProgress();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadStudentsProgress() {
    this.isLoadingStudents = true;
    this.http.get<any[]>(`${this.apiUrl}/attendance/students-progress`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.studentsProgress = data;
        this.filteredProgress = data;
        this.isLoadingStudents = false;
        const sprId = this.route.snapshot.queryParamMap.get('spr');
        if (sprId) {
          const found = data.find(s => s.sprId === +sprId);
          if (found) this.selectStudent(found);
        }
      },
      error: () => { this.isLoadingStudents = false; }
    });
  }

  searchStudents() {
    const term = this.searchTerm.toLowerCase();
    if (!term) { this.filteredProgress = this.studentsProgress; return; }
    this.filteredProgress = this.studentsProgress.filter(s =>
      s.studentName?.toLowerCase().includes(term) ||
      s.nic?.toLowerCase().includes(term) ||
      s.phone?.toLowerCase().includes(term)
    );
  }

  selectStudent(s: any) {
    this.selected = s;
    this.form.studentPackageRegistrationId = s.sprId;
    this.form.isReadyForPracticalTest = false;
    this.successMessage = '';
    this.errorMessage = '';
    this.loadAttendanceRecords(s.sprId);
  }

  loadAttendanceRecords(sprId: number) {
    this.isLoadingRecords = true;
    this.http.get<any[]>(`${this.apiUrl}/attendance/${sprId}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.attendanceRecords = data;
        this.form.dayNumber = data.length + 1;
        this.isLoadingRecords = false;
      },
      error: () => { this.isLoadingRecords = false; }
    });
  }

  save() {
    if (!this.form.studentPackageRegistrationId) { this.errorMessage = 'Please select a student.'; return; }
    this.isSaving = true;
    this.errorMessage = '';

    this.http.post(`${this.apiUrl}/attendance`, this.form, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = `Day ${this.form.dayNumber} recorded!`;
        this.loadAttendanceRecords(this.form.studentPackageRegistrationId);
        this.loadStudentsProgress();
        this.form.notes = '';
        this.form.isReadyForPracticalTest = false;
        setTimeout(() => { this.successMessage = ''; }, 4000);
      },
      error: (err: any) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'Failed to record attendance.';
      }
    });
  }

  requestDeleteRecord(id: number)  { this.deleteConfirmId = id; }
  cancelDeleteRecord()             { this.deleteConfirmId = null; }

  deleteRecord() {
    if (!this.deleteConfirmId) return;
    const id = this.deleteConfirmId;
    this.deleteConfirmId = null;
    this.http.delete(`${this.apiUrl}/attendance/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.attendanceRecords = this.attendanceRecords.filter(r => r.id !== id);
        this.form.dayNumber    = this.attendanceRecords.length + 1;
        this.loadStudentsProgress();
      }
    });
  }

  get progressPercent() {
    if (!this.selected) return 0;
    return Math.min(100, Math.round((this.attendanceRecords.length / 15) * 100));
  }
  get daysLeft() { return Math.max(0, 15 - this.attendanceRecords.length); }
}
