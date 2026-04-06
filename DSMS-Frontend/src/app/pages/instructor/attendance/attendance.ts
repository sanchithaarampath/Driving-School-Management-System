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
  students: any[] = [];
  selectedSprId: number | null = null;
  attendanceRecords: any[] = [];
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  private apiUrl = 'http://localhost:5062/api';

  form = {
    studentPackageRegistrationId: 0,
    instructorId: null as number | null,
    attendanceDate: new Date().toISOString().split('T')[0],
    dayNumber: 1,
    notes: '',
    isReadyForPracticalTest: false
  };

  constructor(private auth: AuthService, private http: HttpClient, private route: ActivatedRoute) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadStudents();
    const sprId = this.route.snapshot.queryParamMap.get('spr');
    if (sprId) { this.selectedSprId = +sprId; this.loadAttendance(+sprId); }
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadStudents() {
    this.http.get(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.students = data; }
    });
  }

  selectStudent(sprId: number) {
    this.selectedSprId = sprId;
    this.form.studentPackageRegistrationId = sprId;
    this.loadAttendance(sprId);
  }

  loadAttendance(sprId: number) {
    this.isLoading = true;
    this.http.get(`${this.apiUrl}/attendance/${sprId}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.attendanceRecords = data; this.form.dayNumber = data.length + 1; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  save() {
    if (!this.form.studentPackageRegistrationId) { this.errorMessage = 'Please select a student'; return; }
    this.isSaving = true;
    this.errorMessage = '';
    this.http.post(`${this.apiUrl}/attendance`, this.form, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = 'Attendance recorded!';
        this.loadAttendance(this.form.studentPackageRegistrationId);
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: (err: any) => { this.isSaving = false; this.errorMessage = err.error?.message || 'Error saving'; }
    });
  }
}
