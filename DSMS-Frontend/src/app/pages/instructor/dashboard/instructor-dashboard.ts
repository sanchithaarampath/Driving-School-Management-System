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
  isLoading = true;
  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient, private router: Router) {}

  ngOnInit() {
    this.user = this.auth.getUser();
    this.loadStudentsProgress();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadStudentsProgress() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/attendance/students-progress`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.studentsProgress = data; this.filteredStudents = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
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
  get completedCount()  { return this.studentsProgress.filter(s => s.attendanceDays >= 15).length; }
  get inProgressCount() { return this.studentsProgress.filter(s => s.attendanceDays > 0 && s.attendanceDays < 15).length; }

  progressPercent(days: number) { return Math.min(100, Math.round((days / 15) * 100)); }
  goToAttendance() { this.router.navigate(['/instructor/attendance']); }
  goToAttendanceFor(sprId: number) { this.router.navigate(['/instructor/attendance'], { queryParams: { spr: sprId } }); }
}
