import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, TopbarComponent],
  templateUrl: './instructor-dashboard.html',
  styleUrl: './instructor-dashboard.scss'
})
export class InstructorDashboard implements OnInit {
  user: any;
  studentsProgress: any[] = [];
  filteredStudents: any[] = [];
  searchTerm = '';
  isLoading  = true;
  activeTab: 'students' | 'schedule' = 'students';
  private apiUrl = 'http://localhost:5062/api';

  // My Schedule
  sessions:        any[] = [];
  scheduleLoading  = false;
  scheduleFilter   = '';       // '' | 'Scheduled' | 'Completed' | 'Cancelled'

  // Complete / Cancel inline
  completingId     = 0;
  cancellingId     = 0;
  hoursInput       = 1;
  notesInput       = '';
  cancelReason     = '';
  cancelStatus     = 'Cancelled';

  constructor(private auth: AuthService, private http: HttpClient, private router: Router) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadStudentsProgress();
    this.loadSchedule();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadStudentsProgress() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/attendance/students-progress`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.studentsProgress = data; this.filteredStudents = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  loadSchedule() {
    this.scheduleLoading = true;
    const qs = this.scheduleFilter ? `?status=${this.scheduleFilter}` : '';
    this.http.get<any[]>(`${this.apiUrl}/training-sessions/my-schedule${qs}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.sessions = data; this.scheduleLoading = false; },
      error: () => { this.scheduleLoading = false; }
    });
  }

  switchTab(tab: 'students' | 'schedule') {
    this.activeTab = tab;
    if (tab === 'schedule') this.loadSchedule();
  }

  search() {
    const term = this.searchTerm.toLowerCase();
    if (!term) { this.filteredStudents = this.studentsProgress; return; }
    this.filteredStudents = this.studentsProgress.filter(s =>
      s.studentName?.toLowerCase().includes(term) ||
      s.nic?.toLowerCase().includes(term)
    );
  }

  get totalStudents()   { return this.studentsProgress.length; }
  get approvedCount()   { return this.studentsProgress.filter(s => s.isRecommendForTrial).length; }
  get completedCount()  { return this.studentsProgress.filter(s => s.attendanceDays > 0 && s.totalSessions > 0 && s.attendanceDays >= s.totalSessions).length; }
  get inProgressCount() { return this.studentsProgress.filter(s => s.attendanceDays > 0).length; }
  get scheduledCount()  { return this.sessions.filter(s => s.status === 'Scheduled').length; }

  progressPercent(days: number) { return Math.min(100, Math.round((days / 15) * 100)); }
  sessionPercent(s: any): number {
    if (s.totalSessions > 0) return Math.min(100, Math.round((s.attendanceDays / s.totalSessions) * 100));
    if (s.attendanceDays > 0) return Math.min(100, Math.round((s.attendanceDays / 15) * 100));
    return 0;
  }
  goToAttendance() { this.router.navigate(['/instructor/attendance']); }
  goToAttendanceFor(sprId: number) { this.router.navigate(['/instructor/attendance'], { queryParams: { spr: sprId } }); }

  openComplete(session: any) {
    this.completingId = session.id;
    this.hoursInput   = session.durationHours;
    this.notesInput   = '';
    this.cancellingId = 0;
  }

  openCancel(session: any) {
    this.cancellingId = session.id;
    this.cancelReason = '';
    this.cancelStatus = 'Cancelled';
    this.completingId = 0;
  }

  submitComplete(sessionId: number) {
    const payload = { hoursLogged: this.hoursInput, sessionNotes: this.notesInput };
    this.http.put(`${this.apiUrl}/training-sessions/${sessionId}/complete`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => { this.completingId = 0; this.loadSchedule(); },
      error: (err) => alert(err.error?.message || 'Failed to complete session.')
    });
  }

  submitCancel(sessionId: number) {
    const payload = { status: this.cancelStatus, cancelReason: this.cancelReason };
    this.http.put(`${this.apiUrl}/training-sessions/${sessionId}/cancel`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => { this.cancellingId = 0; this.loadSchedule(); },
      error: (err) => alert(err.error?.message || 'Failed to cancel session.')
    });
  }

  statusClass(status: string) {
    switch (status) {
      case 'Scheduled': return 'badge-scheduled';
      case 'Completed': return 'badge-completed';
      case 'Cancelled': return 'badge-cancelled';
      case 'NoShow':    return 'badge-noshow';
      default:          return '';
    }
  }

  isOverdue(session: any): boolean {
    if (session.status !== 'Scheduled') return false;
    return new Date(session.scheduledDate) < new Date(new Date().toDateString());
  }
}
