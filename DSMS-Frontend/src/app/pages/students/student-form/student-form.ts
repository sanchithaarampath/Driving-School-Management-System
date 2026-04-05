import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './student-form.html',
  styleUrl: './student-form.scss'
})
export class StudentForm implements OnInit {
  isEditMode = false;
  studentId: number | null = null;
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  user: any;
  branches: any[] = [];
  private apiUrl = 'http://localhost:5062/api';

  student = {
    branchId: 1,
    studentName: '',
    email: '',
    phoneNumber: '',
    whatsAppNumber: '',
    address: '',
    nic: '',
    dob: '',
    gender: '',
    nearestPoliceStation: '',
    nearestDivisionalSecretariat: '',
    existingLicenseNo: '',
    isSpecialRequirements: false,
    specialRequirementTypeId: null as number | null
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadBranches();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.studentId = +id;
      this.loadStudent(this.studentId);
    }
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  loadStudent(id: number) {
    this.isLoading = true;
    this.http.get(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        this.student = {
          branchId: data.branchId,
          studentName: data.studentName,
          email: data.email || '',
          phoneNumber: data.phoneNumber,
          whatsAppNumber: data.whatsAppNumber || '',
          address: data.address,
          nic: data.nic,
          dob: data.dob ? data.dob.split('T')[0] : '',
          gender: data.gender || '',
          nearestPoliceStation: data.nearestPoliceStation || '',
          nearestDivisionalSecretariat: data.nearestDivisionalSecretariat || '',
          existingLicenseNo: data.existingLicenseNo || '',
          isSpecialRequirements: data.isSpecialRequirements || false,
          specialRequirementTypeId: data.specialRequirementTypeId || null
        };
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  onSubmit() {
    if (!this.student.studentName || !this.student.nic || !this.student.phoneNumber || !this.student.address) {
      this.errorMessage = 'Please fill all required fields';
      return;
    }
    if (!this.isEditMode && !this.student.dob) {
      this.errorMessage = 'Please enter date of birth';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const request = this.isEditMode
      ? this.http.put(`${this.apiUrl}/student/${this.studentId}`, this.student, { headers: this.getHeaders() })
      : this.http.post(`${this.apiUrl}/student`, this.student, { headers: this.getHeaders() });

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = this.isEditMode ? 'Student updated successfully!' : 'Student registered successfully!';
        setTimeout(() => this.router.navigate(['/students']), 1500);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  goBack() { this.router.navigate(['/students']); }
  logout() { this.authService.logout(); }
}