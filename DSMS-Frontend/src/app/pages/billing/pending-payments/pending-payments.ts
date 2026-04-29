import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-pending-payments',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, TopbarComponent],
  templateUrl: './pending-payments.html',
  styleUrl: './pending-payments.scss'
})
export class PendingPayments implements OnInit {
  bills: any[] = [];
  filteredBills: any[] = [];
  searchTerm = '';
  isLoading = true;
  private apiUrl = 'http://localhost:5062/api';

  // ── Payment modal ──────────────────────────────────────────────────────────
  showPaymentModal = false;
  paymentBill: any = null;
  paymentForm = { amount: 0, paymentMethod: 'Cash', referenceNo: '', remarks: '' };
  paymentMethods = ['Cash', 'Bank Transfer', 'Cheque', 'Card (POS)'];
  isSavingPayment = false;
  paymentSuccess = '';
  paymentError = '';

  // ── Receipt modal ──────────────────────────────────────────────────────────
  showReceiptModal = false;
  selectedBill: any = null;
  overrideEmail = '';
  overridePhone = '';
  receiptSending = false;
  receiptSuccess = '';
  receiptError = '';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  ngOnInit() { this.loadPending(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadPending() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/billing/pending`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.bills = data; this.filteredBills = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  search() {
    if (!this.searchTerm) { this.filteredBills = this.bills; return; }
    const term = this.searchTerm.toLowerCase();
    this.filteredBills = this.bills.filter(b =>
      b.studentName?.toLowerCase().includes(term) ||
      b.billNumber?.toLowerCase().includes(term) ||
      b.studentNic?.toLowerCase().includes(term) ||
      b.studentPhone?.toLowerCase().includes(term)
    );
  }

  get totalOutstanding() { return this.bills.reduce((sum, b) => sum + b.balanceAmount, 0); }

  getStatusClass(status: string) {
    if (status === 'Paid')    return 'badge-paid';
    if (status === 'Partial') return 'badge-partial';
    return 'badge-pending';
  }

  // ── Payment ────────────────────────────────────────────────────────────────
  openPaymentModal(bill: any) {
    this.paymentBill = bill;
    this.paymentForm = { amount: bill.balanceAmount, paymentMethod: 'Cash', referenceNo: '', remarks: '' };
    this.paymentSuccess = '';
    this.paymentError = '';
    this.showPaymentModal = true;
  }

  closePaymentModal() { this.showPaymentModal = false; this.paymentBill = null; }

  savePayment() {
    if (!this.paymentBill || !this.paymentForm.amount) { this.paymentError = 'Enter a valid amount.'; return; }
    if (this.paymentForm.amount > this.paymentBill.balanceAmount) {
      this.paymentError = `Amount cannot exceed balance of Rs. ${this.paymentBill.balanceAmount.toLocaleString()}`; return;
    }
    this.isSavingPayment = true;
    this.paymentError = '';
    this.http.post<any>(`${this.apiUrl}/billing/${this.paymentBill.id}/payment`, this.paymentForm, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSavingPayment = false;
        this.paymentSuccess = 'Payment recorded successfully!';
        this.loadPending();
        setTimeout(() => { this.closePaymentModal(); }, 1500);
      },
      error: (err) => { this.isSavingPayment = false; this.paymentError = err.error?.message || 'Failed to save payment.'; }
    });
  }

  // ── Receipt ────────────────────────────────────────────────────────────────
  openReceiptModal(bill: any) {
    this.selectedBill = bill;
    this.overrideEmail = '';
    this.overridePhone = '';
    this.receiptSuccess = '';
    this.receiptError = '';
    this.showReceiptModal = true;
  }

  closeReceiptModal() { this.showReceiptModal = false; this.selectedBill = null; }

  sendReceipt(channel: 'email' | 'whatsapp' | 'both') {
    if (!this.selectedBill) return;
    this.receiptSending = true;
    this.receiptSuccess = '';
    this.receiptError = '';
    const payload = {
      sendEmail:    channel === 'email' || channel === 'both',
      sendWhatsApp: channel === 'whatsapp' || channel === 'both',
      overrideEmail: this.overrideEmail || null,
      overridePhone: this.overridePhone || null
    };
    this.http.post<any>(`${this.apiUrl}/billing/${this.selectedBill.id}/send-receipt`, payload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.receiptSending = false;
        this.receiptSuccess = res.sent?.join(' • ') ?? 'Sent!';
        if (res.errors?.length) this.receiptError = res.errors.join(' • ');
      },
      error: () => { this.receiptSending = false; this.receiptError = 'Failed to send reminder.'; }
    });
  }

  goToBilling() { this.router.navigate(['/billing']); }
}
