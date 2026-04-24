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
  students: any[] = [];
  filteredStudents: any[] = [];
  searchTerm = '';
  isLoading = true;
  isSearching = false;
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadStudents();
  }

  get isInstructor() { return this.authService.isInstructor(); }
  get canEdit() { return !this.authService.isInstructor(); }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadStudents() {
    this.isLoading = true;
    this.http.get(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        this.students = data;
        this.filteredStudents = data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  search() {
    const term = this.searchTerm.trim();
    if (!term) {
      this.filteredStudents = this.students;
      return;
    }

    // If term looks like a bill number — search via API
    if (term.toUpperCase().startsWith('BILL-') || /^\d{4,}$/.test(term)) {
      this.isSearching = true;
      this.http.get<any[]>(`${this.apiUrl}/student/search?q=${encodeURIComponent(term)}`, { headers: this.getHeaders() }).subscribe({
        next: (data) => { this.filteredStudents = data; this.isSearching = false; },
        error: () => { this.isSearching = false; }
      });
      return;
    }

    // Client-side search by name, NIC, phone
    const lower = term.toLowerCase();
    this.filteredStudents = this.students.filter(s =>
      s.studentName?.toLowerCase().includes(lower) ||
      s.nic?.toLowerCase().includes(lower) ||
      s.phoneNumber?.toLowerCase().includes(lower)
    );
  }

  clearSearch() {
    this.searchTerm = '';
    this.filteredStudents = this.students;
  }

  viewProfile(id: number) { this.router.navigate(['/students/profile', id]); }
  addStudent()            { this.router.navigate(['/students/new']); }
  editStudent(id: number) { this.router.navigate(['/students/edit', id]); }
}
