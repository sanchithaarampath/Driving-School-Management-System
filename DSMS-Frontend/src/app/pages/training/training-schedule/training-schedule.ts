import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-training-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './training-schedule.html',
  styleUrl: './training-schedule.scss'
})
export class TrainingSchedulePage implements OnInit {
  private apiUrl = 'http://localhost:5062/api';

  sessions:     any[] = [];
  isLoading     = true;
  errorMessage  = '';

  // Filters
  filterDate        = '';
  filterInstructorId = 0;
  filterStatus      = '';
  instructors:      any[] = [];

  // Confirm cancel modal
  cancelSessionId   = 0;
  cancelReason      = '';
  cancelStatus      = 'Cancelled';
  isCancelling      = false;

  get role()       { return this.authService.getRole(); }
  get isInstructor(){ return this.role === 'Instructor'; }
  get isStaff()    { return ['Staff', 'OfficeStaff', 'Branch Admin', 'Company Admin', 'Admin'].includes(this.role ?? ''); }

  constructor(
    private authService: AuthService,
    private router:      Router,
    private http:        HttpClient
  ) {}

  ngOnInit() {
    this.loadInstructors();
    this.loadSessions();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadInstructors() {
    this.http.get<any[]>(`${this.apiUrl}/training-sessions/instructors`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.instructors = data; },
      error: () => {}
    });
  }

  loadSessions() {
    this.isLoading = true;
    const params: string[] = [];
    if (this.filterDate)          params.push(`date=${this.filterDate}`);
    if (this.filterInstructorId)  params.push(`instructorId=${this.filterInstructorId}`);
    if (this.filterStatus)        params.push(`status=${this.filterStatus}`);
    const qs = params.length ? '?' + params.join('&') : '';

    this.http.get<any[]>(`${this.apiUrl}/training-sessions${qs}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.sessions = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load sessions.'; this.isLoading = false; }
    });
  }

  applyFilters() { this.loadSessions(); }
  clearFilters() {
    this.filterDate = '';
    this.filterInstructorId = 0;
    this.filterStatus = '';
    this.loadSessions();
  }

  newBooking() { this.router.navigate(['/training-schedule/new']); }

  viewStudent(studentId: number) { this.router.navigate(['/students/profile', studentId]); }

  openCancelModal(session: any) {
    this.cancelSessionId = session.id;
    this.cancelReason    = '';
    this.cancelStatus    = 'Cancelled';
  }

  confirmCancel() {
    if (!this.cancelSessionId) return;
    this.isCancelling = true;
    const payload = { status: this.cancelStatus, cancelReason: this.cancelReason };
    this.http.put(`${this.apiUrl}/training-sessions/${this.cancelSessionId}/cancel`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isCancelling    = false;
        this.cancelSessionId = 0;
        this.loadSessions();
      },
      error: () => { this.isCancelling = false; }
    });
  }

  markComplete(session: any) {
    const hours = parseFloat(prompt(`Mark session #${session.id} complete. Hours logged?`, String(session.durationHours)) ?? '');
    if (isNaN(hours) || hours <= 0) return;
    const notes = prompt('Session notes (optional):') ?? '';
    const payload = { hoursLogged: hours, sessionNotes: notes };
    this.http.put(`${this.apiUrl}/training-sessions/${session.id}/complete`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => this.loadSessions(),
      error: (err) => alert(err.error?.message || 'Failed to update session.')
    });
  }

  deleteSession(session: any) {
    if (!confirm(`Delete session #${session.id} for ${session.studentName}?`)) return;
    this.http.delete(`${this.apiUrl}/training-sessions/${session.id}`, { headers: this.getHeaders() }).subscribe({
      next: () => this.loadSessions(),
      error: (err) => alert(err.error?.message || 'Delete failed.')
    });
  }

  statusClass(status: string) {
    switch (status) {
      case 'Scheduled':  return 'badge-scheduled';
      case 'Completed':  return 'badge-completed';
      case 'Cancelled':  return 'badge-cancelled';
      case 'NoShow':     return 'badge-noshow';
      default:           return 'badge-secondary';
    }
  }

  isOverdue(session: any): boolean {
    if (session.status !== 'Scheduled') return false;
    return new Date(session.scheduledDate) < new Date(new Date().toDateString());
  }
}
