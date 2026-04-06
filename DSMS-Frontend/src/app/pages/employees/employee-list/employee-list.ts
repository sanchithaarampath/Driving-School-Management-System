import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth';
import { EmployeeService } from '../../../services/employee';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.scss'
})
export class EmployeeList implements OnInit {
  employees: any[] = [];
  filteredEmployees: any[] = [];
  searchTerm = '';
  isLoading = true;
  user: any;

  constructor(
    private authService: AuthService,
    private employeeService: EmployeeService,
    private router: Router
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadEmployees();
  }

  loadEmployees() {
    this.isLoading = true;
    this.employeeService.getAll().subscribe({
      next: (data: any) => {
        this.employees = data;
        this.filteredEmployees = data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  search() {
    if (!this.searchTerm) {
      this.filteredEmployees = this.employees;
      return;
    }
    const term = this.searchTerm.toLowerCase();
    this.filteredEmployees = this.employees.filter(e =>
      e.employeeName?.toLowerCase().includes(term) ||
      e.nic?.toLowerCase().includes(term) ||
      e.department?.toLowerCase().includes(term) ||
      e.designation?.toLowerCase().includes(term)
    );
  }

  addEmployee() { this.router.navigate(['/employees/new']); }
  editEmployee(id: number) { this.router.navigate(['/employees/edit', id]); }
  logout() { this.authService.logout(); }
}
