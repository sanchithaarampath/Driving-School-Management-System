import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-billing-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, SidebarComponent, TopbarComponent],
  templateUrl: './billing-list.html',
  styleUrl: './billing-list.scss'
})
export class BillingList implements OnInit {
  bills: any[] = [];
  filteredBills: any[] = [];
  searchTerm = '';
  isLoading = true;
  summary = { totalAmount: 0, paidAmount: 0, pendingAmount: 0, totalBills: 0 };
  monthlyData: any[] = [];
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  // ── Receipt modal ──────────────────────────────────────────────────────────
  showReceiptModal = false;
  selectedBill: any = null;
  overrideEmail = '';
  overridePhone = '';
  receiptSending = false;
  receiptSuccess = '';
  receiptError = '';

  // ── Add Payment modal ──────────────────────────────────────────────────────
  showPaymentModal = false;
  paymentBill: any = null;
  paymentForm = { amount: 0, paymentMethod: 'Cash', referenceNo: '', remarks: '' };
  paymentMethods = ['Cash', 'Bank Transfer', 'Cheque', 'Card (POS)'];
  isSavingPayment = false;
  paymentSuccess = '';
  paymentError = '';

  constructor(private authService: AuthService, private router: Router, private http: HttpClient) {}

  get isStaff()        { return this.authService.isStaff(); }
  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadBills();
    // Revenue summary & chart only for admins — not for staff
    if (!this.isStaff) {
      this.loadSummary();
      this.loadMonthly();
    }
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadBills() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/billing`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.bills = data; this.filteredBills = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  loadSummary() {
    this.http.get<any>(`${this.apiUrl}/billing/summary`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.summary = data; }
    });
  }

  loadMonthly() {
    this.http.get<any[]>(`${this.apiUrl}/billing/monthly-summary`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.monthlyData = data; }
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
    if (status === 'Paid')    return 'badge-paid';
    if (status === 'Partial') return 'badge-partial';
    return 'badge-pending';
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
      error: () => { this.receiptSending = false; this.receiptError = 'Failed to send receipt.'; }
    });
  }

  previewReceipt(billId: number) {
    this.http.get(`${this.apiUrl}/billing/${billId}/receipt`, {
      headers: this.getHeaders(), responseType: 'text'
    }).subscribe({
      next: (html) => {
        const withControls = html.replace('</body>',
          `<div style="position:fixed;top:1rem;right:1rem;z-index:9999;display:flex;gap:0.5rem;font-family:sans-serif;">
            <button onclick="window.print()"
              style="background:#e63946;color:#fff;border:none;border-radius:6px;padding:0.5rem 1.2rem;font-size:0.875rem;cursor:pointer;font-weight:600;box-shadow:0 2px 8px rgba(0,0,0,0.25);">
              🖨️ Print / Save as PDF
            </button>
            <button onclick="window.close()"
              style="background:#30363d;color:#e6edf3;border:none;border-radius:6px;padding:0.5rem 1rem;font-size:0.875rem;cursor:pointer;">
              ✕ Close
            </button>
          </div></body>`);
        const w = window.open('', '_blank');
        if (w) { w.document.write(withControls); w.document.close(); }
      }
    });
  }

  downloadReceipt(billId: number, billNumber: string) {
    this.http.get(`${this.apiUrl}/billing/${billId}/receipt`, {
      headers: this.getHeaders(), responseType: 'text'
    }).subscribe({
      next: (html) => {
        const blob = new Blob([html], { type: 'text/html' });
        const url  = URL.createObjectURL(blob);
        const a    = document.createElement('a');
        a.href     = url;
        a.download = `${billNumber || 'receipt'}.html`;
        a.click();
        URL.revokeObjectURL(url);
      }
    });
  }

  // ── Add Payment ────────────────────────────────────────────────────────────
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
        this.loadBills();
        this.loadSummary();
        setTimeout(() => { this.closePaymentModal(); }, 1500);
      },
      error: (err) => { this.isSavingPayment = false; this.paymentError = err.error?.message || 'Failed to save payment.'; }
    });
  }

  addBill() { this.router.navigate(['/billing/new']); }
  goToPending() { this.router.navigate(['/billing/pending']); }

  get maxBarValue() {
    if (!this.monthlyData.length) return 1;
    return Math.max(...this.monthlyData.map(m => m.revenue), 1);
  }
}
