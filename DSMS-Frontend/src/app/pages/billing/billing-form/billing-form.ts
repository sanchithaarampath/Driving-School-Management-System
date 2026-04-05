import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-billing-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './billing-form.html',
  styleUrl: './billing-form.scss'
})
export class BillingForm implements OnInit {
  students: any[] = [];
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  bill = {
    studentId: null as number | null,
    totalAmount: 0,
    discountAmount: 0,
    netAmount: 0,
    paidAmount: 0,
    remarks: ''
  };

  payment = {
    amount: 0,
    paymentMethod: 'Cash',
    referenceNo: '',
    remarks: '',
    studentId: null as number | null
  };

  selectedStudent: any = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadStudents();
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadStudents() {
    this.http.get(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.students = data; }
    });
  }

  onStudentChange() {
    this.selectedStudent = this.students.find(s => s.id == this.bill.studentId);
    this.payment.studentId = this.bill.studentId;
  }

  calculateNet() {
    this.bill.netAmount = this.bill.totalAmount - this.bill.discountAmount;
    if (this.bill.netAmount < 0) this.bill.netAmount = 0;
  }

  onSubmit() {
    if (!this.bill.studentId || this.bill.totalAmount <= 0) {
      this.errorMessage = 'Please select a student and enter a valid amount';
      return;
    }
    if (this.payment.amount > this.bill.netAmount) {
      this.errorMessage = 'Payment amount cannot exceed net amount';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.bill.paidAmount = this.payment.amount;

    const billPayload = {
      studentId: this.bill.studentId,
      totalAmount: this.bill.totalAmount,
      discountAmount: this.bill.discountAmount,
      netAmount: this.bill.netAmount,
      paidAmount: this.bill.paidAmount,
      remarks: this.bill.remarks
    };

    this.http.post(`${this.apiUrl}/billing`, billPayload, { headers: this.getHeaders() }).subscribe({
      next: (res: any) => {
        if (this.payment.amount > 0) {
          const paymentPayload = {
            amount: this.payment.amount,
            paymentMethod: this.payment.paymentMethod,
            referenceNo: this.payment.referenceNo,
            remarks: this.payment.remarks,
            studentId: this.bill.studentId
          };
          this.http.post(`${this.apiUrl}/billing/${res.id}/payment`, paymentPayload, { headers: this.getHeaders() }).subscribe({
            next: () => {
              this.isSaving = false;
              this.successMessage = `Bill ${res.billNumber} created and payment recorded!`;
              setTimeout(() => this.router.navigate(['/billing']), 2000);
            },
            error: () => {
              this.isSaving = false;
              this.successMessage = `Bill ${res.billNumber} created! Payment recording failed.`;
              setTimeout(() => this.router.navigate(['/billing']), 2000);
            }
          });
        } else {
          this.isSaving = false;
          this.successMessage = `Bill ${res.billNumber} created successfully!`;
          setTimeout(() => this.router.navigate(['/billing']), 2000);
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  get balanceAmount(): number {
    return this.bill.netAmount - this.payment.amount;
  }

  goBack() { this.router.navigate(['/billing']); }
  logout() { this.authService.logout(); }
}