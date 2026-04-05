import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-billing-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './billing-list.html',
  styleUrl: './billing-list.scss'
})
export class BillingList implements OnInit {
  bills: any[] = [];
  filteredBills: any[] = [];
  searchTerm = '';
  isLoading = true;
  summary = { totalAmount: 0, paidAmount: 0, pendingAmount: 0, totalBills: 0 };
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadBills();
    this.loadSummary();
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadBills() {
    this.isLoading = true;
    this.http.get(`${this.apiUrl}/billing`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        this.bills = data;
        this.filteredBills = data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  loadSummary() {
    this.http.get(`${this.apiUrl}/billing/summary`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.summary = data; }
    });
  }

  search() {
    if (!this.searchTerm) { this.filteredBills = this.bills; return; }
    const term = this.searchTerm.toLowerCase();
    this.filteredBills = this.bills.filter(b =>
      b.studentName?.toLowerCase().includes(term) ||
      b.billNumber?.toLowerCase().includes(term) ||
      b.studentNic?.toLowerCase().includes(term)
    );
  }

  getStatusClass(status: string) {
    if (status === 'Paid') return 'badge-paid';
    if (status === 'Partial') return 'badge-partial';
    return 'badge-pending';
  }

  addBill() { this.router.navigate(['/billing/new']); }
  logout() { this.authService.logout(); }
}