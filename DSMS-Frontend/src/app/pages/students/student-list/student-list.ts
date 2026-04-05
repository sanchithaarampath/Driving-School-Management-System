import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './student-list.html',
  styleUrl: './student-list.scss'
})
export class StudentList implements OnInit {
  students: any[] = [];
  filteredStudents: any[] = [];
  searchTerm = '';
  isLoading = true;
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadStudents();
  }

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
    if (!this.searchTerm) {
      this.filteredStudents = this.students;
      return;
    }
    const term = this.searchTerm.toLowerCase();
    this.filteredStudents = this.students.filter(s =>
      s.studentName?.toLowerCase().includes(term) ||
      s.nic?.toLowerCase().includes(term) ||
      s.phoneNumber?.toLowerCase().includes(term)
    );
  }

  addStudent() { this.router.navigate(['/students/new']); }
  editStudent(id: number) { this.router.navigate(['/students/edit', id]); }
  logout() { this.authService.logout(); }
  goToDashboard() { this.router.navigate(['/dashboard']); }
  goToBilling() { this.router.navigate(['/billing']); }
}