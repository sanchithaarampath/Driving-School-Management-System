import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { EmployeeService } from '../../../services/employee';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './employee-form.html',
  styleUrl: './employee-form.scss'
})
export class EmployeeForm implements OnInit {
  isEditMode = false;
  employeeId: number | null = null;
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  user: any;
  branches: any[] = [];
  private apiUrl = 'http://localhost:5062/api';

  employee = {
    branchId: 1,
    userId: null as number | null,
    employeeName: '',
    nic: '',
    phone: '',
    email: '',
    designation: '',
    department: '',
    joinDate: '',
    address: '',
    emergencyContact: ''
  };

  constructor(
    private authService: AuthService,
    private employeeService: EmployeeService,
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
      this.employeeId = +id;
      this.loadEmployee(this.employeeId);
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

  loadEmployee(id: number) {
    this.isLoading = true;
    this.employeeService.getById(id).subscribe({
      next: (data: any) => {
        this.employee = {
          branchId: data.branchId,
          userId: data.userId || null,
          employeeName: data.employeeName,
          nic: data.nic,
          phone: data.phone,
          email: data.email || '',
          designation: data.designation || '',
          department: data.department || '',
          joinDate: data.joinDate ? data.joinDate.split('T')[0] : '',
          address: data.address || '',
          emergencyContact: data.emergencyContact || ''
        };
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  onSubmit() {
    if (!this.employee.employeeName || !this.employee.nic || !this.employee.phone) {
      this.errorMessage = 'Please fill all required fields';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const request = this.isEditMode
      ? this.employeeService.update(this.employeeId!, this.employee)
      : this.employeeService.create(this.employee);

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.successMessage = this.isEditMode ? 'Employee updated successfully!' : 'Employee added successfully!';
        setTimeout(() => this.router.navigate(['/employees']), 1500);
      },
      error: (err: any) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  goBack() { this.router.navigate(['/employees']); }
  logout() { this.authService.logout(); }
}
