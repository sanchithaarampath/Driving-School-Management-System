import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, TopbarComponent],
  templateUrl: './student-profile.html',
  styleUrl: './student-profile.scss'
})
export class StudentProfile implements OnInit {
  student: any = null;
  bills: any[] = [];
  filteredBills: any[] = [];
  billSearch = '';
  isLoading = true;
  isBillsLoading = true;
  errorMessage = '';
  activeTab: 'info' | 'payments' | 'docs' = 'info';
  private apiUrl = 'http://localhost:5062/api';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadStudent(id);
    this.loadBills(id);
  }

  get canEdit() { return !this.authService.isInstructor(); }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadStudent(id: number) {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.student = data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Student not found.'; this.isLoading = false; }
    });
  }

  loadBills(id: number) {
    this.isBillsLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/billing/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.bills = data; this.filteredBills = data; this.isBillsLoading = false; },
      error: () => { this.isBillsLoading = false; }
    });
  }

  searchBills() {
    const term = this.billSearch.trim().toLowerCase();
    if (!term) { this.filteredBills = this.bills; return; }
    this.filteredBills = this.bills.filter(b =>
      b.billNumber?.toLowerCase().includes(term) ||
      b.status?.toLowerCase().includes(term) ||
      b.paymentMethod?.toLowerCase().includes(term)
    );
  }

  getStatusClass(status: string) {
    if (status === 'Paid')    return 'badge-paid';
    if (status === 'Partial') return 'badge-partial';
    return 'badge-pending';
  }

  getPaymentMethodIcon(method: string) {
    switch (method?.toLowerCase()) {
      case 'cash':          return 'bi-cash';
      case 'bank transfer': return 'bi-bank';
      case 'cheque':        return 'bi-journal-check';
      case 'card':          return 'bi-credit-card';
      case 'paypal':        return 'bi-paypal';
      default:              return 'bi-cash';
    }
  }

  get totalPaid()    { return this.bills.reduce((s, b) => s + (b.paidAmount || 0), 0); }
  get totalPending() { return this.bills.reduce((s, b) => s + (b.balanceAmount || 0), 0); }
  get totalBilled()  { return this.bills.reduce((s, b) => s + (b.netAmount || 0), 0); }

  editStudent()   { this.router.navigate(['/students/edit', this.student.id]); }
  goBack()        { this.router.navigate(['/students']); }
  previewReceipt(billId: number) {
    this.http.get(`${this.apiUrl}/billing/${billId}/receipt`, {
      headers: this.getHeaders(), responseType: 'text'
    }).subscribe({
      next: (html) => {
        const w = window.open('', '_blank');
        if (w) { w.document.write(html); w.document.close(); }
      }
    });
  }
}
