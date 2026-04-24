import { Component, OnInit, AfterViewChecked, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

declare var Stripe: any;
declare var paypal: any;

@Component({
  selector: 'app-billing-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './billing-form.html',
  styleUrl: './billing-form.scss'
})
export class BillingForm implements OnInit, AfterViewChecked {
  students: any[] = [];
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  user: any;
  private apiUrl = 'http://localhost:5062/api';

  // ── Bill & Payment ────────────────────────────────────────────────────────
  bill = { studentId: null as number | null, totalAmount: 0, discountAmount: 0, netAmount: 0, paidAmount: 0, remarks: '' };
  payment = { amount: 0, paymentMethod: 'Cash', referenceNo: '', remarks: '', studentId: null as number | null };
  selectedStudent: any = null;

  // ── Payment method tabs ───────────────────────────────────────────────────
  activePaymentTab: 'Cash' | 'Bank Transfer' | 'Cheque' | 'Card' | 'PayPal' = 'Cash';

  // ── Stripe ────────────────────────────────────────────────────────────────
  private stripe: any = null;
  private cardElement: any = null;
  private stripePublishableKey = '';
  stripeCardMounted = false;
  stripeError = '';
  stripeProcessing = false;
  private stripeCardShouldMount = false;

  // ── PayPal ────────────────────────────────────────────────────────────────
  private paypalClientId = '';
  private paypalCurrency = 'USD';
  paypalLoaded = false;
  private paypalRendered = false;
  private paypalShouldRender = false;

  // ── Post-payment receipt ──────────────────────────────────────────────────
  createdBillId: number | null = null;
  createdBillNumber = '';
  showReceiptPanel = false;
  receiptSending = false;
  receiptSuccess = '';
  receiptError = '';
  overrideEmail = '';
  overridePhone = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadStudents();
    this.fetchStripeConfig();
    this.fetchPayPalConfig();
  }

  ngAfterViewChecked() {
    // Mount Stripe card element once the DOM is ready
    if (this.stripeCardShouldMount && !this.stripeCardMounted) {
      const el = document.getElementById('stripe-card-element');
      if (el && this.stripe) {
        this.cardElement = this.stripe.elements().create('card', {
          style: {
            base: { color: '#e6edf3', fontFamily: '"Segoe UI", sans-serif', fontSize: '15px',
                    '::placeholder': { color: '#8b949e' },
                    iconColor: '#e63946' },
            invalid: { color: '#ff6b7a', iconColor: '#ff6b7a' }
          }
        });
        this.cardElement.mount('#stripe-card-element');
        this.cardElement.on('change', (e: any) => { this.stripeError = e.error?.message ?? ''; });
        this.stripeCardMounted = true;
        this.stripeCardShouldMount = false;
        this.cdr.detectChanges();
      }
    }

    // Render PayPal button once container is in DOM
    if (this.paypalShouldRender && !this.paypalRendered && this.paypalLoaded) {
      const container = document.getElementById('paypal-button-container');
      if (container && typeof paypal !== 'undefined') {
        this.renderPayPalButton();
        this.paypalShouldRender = false;
        this.paypalRendered = true;
      }
    }
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadStudents() {
    this.http.get(`${this.apiUrl}/student`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.students = data; }
    });
  }

  fetchStripeConfig() {
    this.http.get<any>(`${this.apiUrl}/payment-gateway/stripe/config`, { headers: this.getHeaders() }).subscribe({
      next: (cfg) => {
        this.stripePublishableKey = cfg.publishableKey;
        if (cfg.publishableKey && !cfg.publishableKey.startsWith('pk_test_YOUR')) {
          this.stripe = Stripe(cfg.publishableKey);
        }
      },
      error: () => {}
    });
  }

  fetchPayPalConfig() {
    this.http.get<any>(`${this.apiUrl}/payment-gateway/paypal/config`, { headers: this.getHeaders() }).subscribe({
      next: (cfg) => {
        this.paypalClientId = cfg.clientId;
        this.paypalCurrency = cfg.currency ?? 'USD';
      },
      error: () => {}
    });
  }

  // ── Student selection ─────────────────────────────────────────────────────
  onStudentChange() {
    this.selectedStudent = this.students.find(s => s.id == this.bill.studentId) ?? null;
    this.payment.studentId = this.bill.studentId;
    this.overrideEmail = this.selectedStudent?.email ?? '';
    this.overridePhone = this.selectedStudent?.whatsAppNumber ?? this.selectedStudent?.phoneNumber ?? '';
  }

  calculateNet() {
    this.bill.netAmount = Math.max(0, this.bill.totalAmount - this.bill.discountAmount);
  }

  get balanceAmount(): number { return Math.max(0, this.bill.netAmount - this.payment.amount); }

  // ── Payment method switching ──────────────────────────────────────────────
  selectPaymentTab(tab: typeof this.activePaymentTab) {
    this.activePaymentTab = tab;
    this.payment.paymentMethod = tab === 'Card' ? 'Card (Stripe)' : tab === 'PayPal' ? 'PayPal' : tab;
    this.payment.amount = 0;

    if (tab === 'Card') {
      this.stripeCardMounted = false;
      this.stripeCardShouldMount = true;
    }
    if (tab === 'PayPal') {
      this.paypalRendered = false;
      this.paypalShouldRender = true;
      this.loadPayPalSdk();
    }
  }

  // ── Stripe flow ───────────────────────────────────────────────────────────
  async payWithStripe() {
    if (!this.stripe || !this.cardElement) { this.stripeError = 'Stripe not initialized'; return; }
    if (this.payment.amount <= 0) { this.stripeError = 'Enter payment amount first'; return; }

    this.stripeProcessing = true;
    this.stripeError = '';

    // 1. Create PaymentIntent on backend
    const intentRes: any = await this.http.post(
      `${this.apiUrl}/payment-gateway/stripe/create-intent`,
      { amount: this.payment.amount, billId: 0, studentName: this.selectedStudent?.studentName },
      { headers: this.getHeaders() }
    ).toPromise().catch(e => { this.stripeError = e.error?.message ?? 'Failed to create payment intent'; return null; });

    if (!intentRes) { this.stripeProcessing = false; return; }

    // 2. Confirm card payment
    const result = await this.stripe.confirmCardPayment(intentRes.clientSecret, {
      payment_method: { card: this.cardElement, billing_details: { name: this.selectedStudent?.studentName ?? '' } }
    });

    if (result.error) {
      this.stripeError = result.error.message;
      this.stripeProcessing = false;
      return;
    }

    // 3. Payment confirmed — set reference to PaymentIntent ID and submit bill
    this.payment.referenceNo = result.paymentIntent.id;
    this.payment.paymentMethod = 'Card (Stripe)';
    this.stripeProcessing = false;
    this.submitBill();
  }

  // ── PayPal flow ───────────────────────────────────────────────────────────
  loadPayPalSdk() {
    if (this.paypalLoaded || typeof paypal !== 'undefined') { this.paypalLoaded = true; return; }
    if (!this.paypalClientId || this.paypalClientId.startsWith('YOUR_')) {
      this.errorMessage = 'PayPal is not configured yet. Please set up your PayPal Client ID.';
      return;
    }
    const script = document.createElement('script');
    script.src = `https://www.paypal.com/sdk/js?client-id=${this.paypalClientId}&currency=${this.paypalCurrency}`;
    script.onload = () => { this.paypalLoaded = true; this.cdr.detectChanges(); };
    document.head.appendChild(script);
  }

  private renderPayPalButton() {
    paypal.Buttons({
      createOrder: (_data: any, actions: any) => {
        return actions.order.create({
          purchase_units: [{ amount: { value: this.payment.amount.toFixed(2) } }]
        });
      },
      onApprove: (_data: any, actions: any) => {
        return actions.order.capture().then((details: any) => {
          this.payment.referenceNo = details.id;
          this.payment.paymentMethod = 'PayPal';
          this.submitBill();
        });
      },
      onError: (err: any) => { this.errorMessage = 'PayPal payment failed: ' + err; }
    }).render('#paypal-button-container');
  }

  // ── Bill submission (Cash / Bank / Cheque) ────────────────────────────────
  onSubmit() {
    if (this.activePaymentTab === 'Card') { this.payWithStripe(); return; }
    if (this.activePaymentTab === 'PayPal') { return; } // PayPal button handles it
    this.submitBill();
  }

  private submitBill() {
    if (!this.bill.studentId || this.bill.totalAmount <= 0) {
      this.errorMessage = 'Please select a student and enter a valid amount';
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

    this.http.post<any>(`${this.apiUrl}/billing`, billPayload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.createdBillId = res.id;
        this.createdBillNumber = res.billNumber;

        if (this.payment.amount > 0) {
          const paymentPayload = {
            amount: this.payment.amount,
            paymentMethod: this.payment.paymentMethod,
            referenceNo: this.payment.referenceNo,
            remarks: this.payment.remarks,
            studentId: this.bill.studentId
          };
          this.http.post(`${this.apiUrl}/billing/${res.id}/payment`, paymentPayload, { headers: this.getHeaders() }).subscribe({
            next: () => { this.isSaving = false; this.showReceiptPanel = true; },
            error: () => { this.isSaving = false; this.successMessage = `Bill ${res.billNumber} created!`; this.showReceiptPanel = true; }
          });
        } else {
          this.isSaving = false;
          this.showReceiptPanel = true;
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  // ── Receipt sending ───────────────────────────────────────────────────────
  sendReceipt(channel: 'email' | 'whatsapp' | 'both') {
    if (!this.createdBillId) return;
    this.receiptSending = true;
    this.receiptSuccess = '';
    this.receiptError = '';

    const payload = {
      sendEmail:    channel === 'email' || channel === 'both',
      sendWhatsApp: channel === 'whatsapp' || channel === 'both',
      overrideEmail: this.overrideEmail || null,
      overridePhone: this.overridePhone || null
    };

    this.http.post<any>(`${this.apiUrl}/billing/${this.createdBillId}/send-receipt`, payload, { headers: this.getHeaders() }).subscribe({
      next: (res) => {
        this.receiptSending = false;
        if (res.sent?.length) this.receiptSuccess = res.sent.join(' • ');
        if (res.errors?.length) this.receiptError = res.errors.join(' • ');
      },
      error: () => { this.receiptSending = false; this.receiptError = 'Failed to send receipt.'; }
    });
  }

  previewReceipt() {
    if (!this.createdBillId) return;
    const token = this.authService.getToken();
    window.open(`${this.apiUrl}/billing/${this.createdBillId}/receipt?token=${token}`, '_blank');
  }

  finishAndGoBack() { this.router.navigate(['/billing']); }
  goBack() { this.router.navigate(['/billing']); }
}
