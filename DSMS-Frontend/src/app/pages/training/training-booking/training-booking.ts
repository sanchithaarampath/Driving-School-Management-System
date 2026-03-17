import { Component, OnInit, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-training-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './training-booking.html',
  styleUrl: './training-booking.scss'
})
export class TrainingBookingPage implements OnInit {
  private apiUrl = 'http://localhost:5062/api';

  isSaving     = false;
  errorMessage = '';
  successMsg   = '';

  // Student search
  allStudents:        any[] = [];
  studentSearch       = '';
  studentResults:     any[] = [];
  showStudentDropdown = false;
  selectedStudent:    any  = null;

  // Instructors dropdown
  instructors: any[] = [];

  // Form fields
  instructorId   = 0;
  scheduledDate  = '';
  scheduledTime  = '09:00';
  durationHours  = 1;
  vehicleClass   = '';
  notes          = '';

  vehicleClasses = ['Class A', 'Class B', 'Class C', 'Class D', 'Class E', 'Class F', 'Class G'];

  private preselectedStudentId: number | null = null;

  constructor(
    private authService: AuthService,
    private router:      Router,
    private route:       ActivatedRoute,
    private http:        HttpClient,
    private elRef:       ElementRef
  ) {}

  ngOnInit() {
    const qid = this.route.snapshot.queryParamMap.get('studentId');
    if (qid) this.preselectedStudentId = +qid;
    this.loadStudents();
    this.loadInstructors();
    // default date = today
    this.scheduledDate = new Date().toISOString().substring(0, 10);
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadStudents() {
    this.http.get<any[]>(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.allStudents = data;
        if (this.preselectedStudentId) {
          const found = data.find(s => s.id === this.preselectedStudentId);
          if (found) this.selectStudent(found);
        }
      }
    });
  }

  loadInstructors() {
    this.http.get<any[]>(`${this.apiUrl}/training-sessions/instructors`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.instructors = data; }
    });
  }

  onStudentInput() {
    const term = this.studentSearch.trim().toLowerCase();
    if (term.length < 2) { this.studentResults = []; this.showStudentDropdown = false; return; }
    this.studentResults = this.allStudents
      .filter(s =>
        s.studentName?.toLowerCase().includes(term) ||
        s.nic?.toLowerCase().includes(term) ||
        s.phoneNumber?.includes(term)
      ).slice(0, 8);
    this.showStudentDropdown = this.studentResults.length > 0;
  }

  selectStudent(s: any) {
    this.selectedStudent     = s;
    this.studentSearch       = `${s.studentName} — ${s.nic}`;
    this.showStudentDropdown = false;
    // Pre-fill vehicle class from student's registered class if available
    if (s.vehicleClasses?.length) this.vehicleClass = s.vehicleClasses[0];
  }

  clearStudent() {
    this.selectedStudent     = null;
    this.studentSearch       = '';
    this.studentResults      = [];
    this.showStudentDropdown = false;
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(e: Event) {
    if (!this.elRef.nativeElement.contains(e.target))
      this.showStudentDropdown = false;
  }

  onSubmit() {
    this.errorMessage = '';
    if (!this.selectedStudent)  { this.errorMessage = 'Please select a student.'; return; }
    if (!this.instructorId)     { this.errorMessage = 'Please select an instructor.'; return; }
    if (!this.scheduledDate)    { this.errorMessage = 'Please select a date.'; return; }
    if (!this.scheduledTime)    { this.errorMessage = 'Please enter a time.'; return; }
    if (!this.vehicleClass)     { this.errorMessage = 'Please select a vehicle class.'; return; }
    if (this.durationHours <= 0){ this.errorMessage = 'Duration must be greater than 0.'; return; }

    this.isSaving = true;
    const payload = {
      studentId:    this.selectedStudent.id,
      instructorId: this.instructorId,
      scheduledDate: this.scheduledDate,
      scheduledTime: this.scheduledTime,
      durationHours: this.durationHours,
      vehicleClass:  this.vehicleClass,
      notes:         this.notes
    };

    this.http.post<any>(`${this.apiUrl}/training-sessions`, payload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.isSaving   = false;
        this.successMsg = `Session booked (ID: ${res.id}). Redirecting...`;
        setTimeout(() => this.router.navigate(['/training-schedule']), 1500);
      },
      error: (err) => {
        this.isSaving     = false;
        this.errorMessage = err.error?.message || 'Failed to book session.';
      }
    });
  }

  getInstructorName(id: number): string {
    return this.instructors.find(i => i.id === id)?.instructorName ?? 'Selected';
  }

  goBack() { this.router.navigate(['/training-schedule']); }
}
