import { Component, OnInit, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-billing-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './billing-form.html',
  styleUrl: './billing-form.scss'
})
export class BillingForm implements OnInit {
  isSaving     = false;
  errorMessage = '';
  private apiUrl = 'http://localhost:5062/api';

  // ── Student Search ────────────────────────────────────────────────────────
  allStudents:         any[] = [];
  studentSearch        = '';
  studentResults:      any[] = [];
  showStudentDropdown  = false;
  selectedStudent:     any  = null;

  // ── Package & Balance (read-only, auto-loaded from student registration) ──
  packageName          = '';
  packagePrice         = 0;
  previouslyPaid       = 0;
  isLoadingBalance     = false;

  get remainingBalance()      { return Math.max(0, this.packagePrice - this.previouslyPaid); }
  get balanceAfterPayment()   { return Math.max(0, this.remainingBalance - this.instalmentAmount); }
  get isFullyPaid()           { return this.packagePrice > 0 && this.previouslyPaid >= this.packagePrice; }

  // ── This Instalment ───────────────────────────────────────────────────────
  instalmentAmount     = 0;
  discountAmount       = 0;
  remarks              = '';

  // ── Payment method ────────────────────────────────────────────────────────
  activePaymentTab: 'Cash' | 'Bank Transfer' | 'Cheque' | 'Card (POS)' = 'Cash';
  paymentMethod        = 'Cash';
  referenceNo          = '';
  paymentRemarks       = '';

  // ── Receipt ───────────────────────────────────────────────────────────────
  createdBillId        = 0;
  createdBillNumber    = '';
  showReceiptPanel     = false;
  receiptSending       = false;
  receiptSuccess       = '';
  receiptError         = '';
  overrideEmail        = '';
  overridePhone        = '';

  private preselectedStudentId: number | null = null;

  constructor(
    private authService: AuthService,
    private router:      Router,
    private route:       ActivatedRoute,
    private http:        HttpClient,
    private elRef:       ElementRef
  ) {}

  ngOnInit() {
    const qid = this.route.snapshot.queryParamMap.get('studentId');
    if (qid) this.preselectedStudentId = +qid;
    this.loadStudents();
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  // ── Student search ────────────────────────────────────────────────────────
  loadStudents() {
    this.http.get<any[]>(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.allStudents = data;
        if (this.preselectedStudentId) {
          const found = data.find(s => s.id === this.preselectedStudentId);
          if (found) this.selectStudent(found);
        }
      }
    });
  }

  onStudentInput() {
    const term = this.studentSearch.trim().toLowerCase();
    if (term.length < 2) { this.studentResults = []; this.showStudentDropdown = false; return; }
    this.studentResults = this.allStudents
      .filter(s =>
        s.studentName?.toLowerCase().includes(term) ||
        s.nic?.toLowerCase().includes(term) ||
        s.phoneNumber?.includes(term)
      ).slice(0, 8);
    this.showStudentDropdown = this.studentResults.length > 0;
  }

  selectStudent(s: any) {
    this.selectedStudent    = s;
    this.studentSearch      = `${s.studentName} — ${s.nic}`;
    this.showStudentDropdown = false;
    this.overrideEmail      = s.email ?? '';
    this.overridePhone      = s.whatsAppNumber ?? s.phoneNumber ?? '';

    // Reset balance state
    this.packageName    = '';
    this.packagePrice   = 0;
    this.previouslyPaid = 0;
    this.instalmentAmount = 0;

    this.isLoadingBalance = true;

    // Load student profile → get registered package price
    this.http.get<any>(`${this.apiUrl}/student/${s.id}`, { headers: this.getHeaders() }).subscribe({
      next: (profile) => {
        this.packageName  = profile.coursePackage?.packageName ?? '';
        this.packagePrice = profile.coursePackage?.price ?? 0;

        // Load previous bills → compute already paid
        this.http.get<any[]>(`${this.apiUrl}/billing/student/${s.id}`, { headers: this.getHeaders() }).subscribe({
          next: (bills) => {
            this.previouslyPaid   = bills.reduce((sum, b) => sum + (b.paidAmount || 0), 0);
            this.instalmentAmount = this.remainingBalance; // pre-fill with remaining
            this.isLoadingBalance = false;
          },
          error: () => {
            this.instalmentAmount = this.packagePrice;
            this.isLoadingBalance = false;
          }
        });
      },
      error: () => { this.isLoadingBalance = false; }
    });
  }

  clearStudent() {
    this.selectedStudent     = null;
    this.studentSearch       = '';
    this.studentResults      = [];
    this.showStudentDropdown = false;
    this.packageName         = '';
    this.packagePrice        = 0;
    this.previouslyPaid      = 0;
    this.instalmentAmount    = 0;
    this.discountAmount      = 0;
    this.remarks             = '';
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(e: Event) {
    if (!this.elRef.nativeElement.contains(e.target))
      this.showStudentDropdown = false;
  }

  // ── Payment method ────────────────────────────────────────────────────────
  selectPaymentTab(tab: typeof this.activePaymentTab) {
    this.activePaymentTab = tab;
    this.paymentMethod    = tab;
    this.referenceNo      = '';
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  onSubmit() {
    if (!this.selectedStudent) { this.errorMessage = 'Please select a student.'; return; }
    if (!this.packagePrice)    { this.errorMessage = 'This student has no registered package.'; return; }
    if (this.isFullyPaid)      { this.errorMessage = 'This student has no remaining balance.'; return; }
    if (this.instalmentAmount <= 0) { this.errorMessage = 'Enter a valid instalment amount.'; return; }
    if (this.instalmentAmount > this.remainingBalance) {
      this.errorMessage = `Amount exceeds remaining balance of Rs. ${this.remainingBalance.toLocaleString()}.`;
      return;
    }

    this.isSaving     = true;
    this.errorMessage = '';

    const payload = {
      studentId:         this.selectedStudent.id,
      packagePrice:      this.packagePrice,
      installmentAmount: this.instalmentAmount,
      discountAmount:    this.discountAmount,
      paymentMethod:     this.paymentMethod,
      referenceNo:       this.referenceNo,
      remarks:           this.remarks || this.paymentRemarks
    };

    this.http.post<any>(`${this.apiUrl}/billing`, payload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.createdBillId     = res.id;
        this.createdBillNumber = res.billNumber;
        this.isSaving          = false;
        this.showReceiptPanel  = true;
      },
      error: (err) => {
        this.isSaving     = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  // ── Receipt ───────────────────────────────────────────────────────────────
  sendReceipt(channel: 'email' | 'whatsapp' | 'both') {
    if (!this.createdBillId) return;
    this.receiptSending = true;
    this.receiptSuccess = '';
    this.receiptError   = '';
    const payload = {
      sendEmail:     channel === 'email'    || channel === 'both',
      sendWhatsApp:  channel === 'whatsapp' || channel === 'both',
      overrideEmail: this.overrideEmail || null,
      overridePhone: this.overridePhone || null
    };
    this.http.post<any>(`${this.apiUrl}/billing/${this.createdBillId}/send-receipt`, payload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.receiptSending = false;
        if (res.sent?.length)   this.receiptSuccess = res.sent.join(' • ');
        if (res.errors?.length) this.receiptError   = res.errors.join(' • ');
      },
      error: () => { this.receiptSending = false; this.receiptError = 'Failed to send receipt.'; }
    });
  }

  previewReceipt() {
    if (!this.createdBillId) return;
    this.http.get(`${this.apiUrl}/billing/${this.createdBillId}/receipt`, {
      headers: this.getHeaders(), responseType: 'text'
    }).subscribe({
      next: (html) => {
        const withControls = html.replace('</body>',
          `<div class="no-print" style="position:fixed;top:1rem;right:1rem;z-index:9999;display:flex;gap:0.5rem;font-family:sans-serif;">
            <button onclick="window.print()" style="background:#e63946;color:#fff;border:none;border-radius:6px;padding:0.5rem 1.2rem;font-size:0.875rem;cursor:pointer;font-weight:600;">
              🖨️ Print / Save as PDF
            </button>
            <button onclick="window.close()" style="background:#30363d;color:#e6edf3;border:none;border-radius:6px;padding:0.5rem 1rem;font-size:0.875rem;cursor:pointer;">
              ✕ Close
            </button>
          </div></body>`);
        const w = window.open('', '_blank');
        if (w) { w.document.write(withControls); w.document.close(); }
      }
    });
  }

  downloadReceiptPdf() {
    if (!this.createdBillId) return;
    this.http.get(`${this.apiUrl}/billing/${this.createdBillId}/receipt`, {
      headers: this.getHeaders(), responseType: 'text'
    }).subscribe({
      next: (html) => {
        const withPrint = html.replace('</body>',
          `<script>window.addEventListener('load',function(){setTimeout(function(){window.print();},300);});</script></body>`);
        const w = window.open('', '_blank');
        if (w) { w.document.write(withPrint); w.document.close(); }
      }
    });
  }

  finishAndGoBack() { this.router.navigate(['/billing']); }
  goBack()          { this.router.navigate(['/billing']); }
}
