import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

const DOC_SLOTS = [
  { key: 'BirthCertificate', label: 'Birth Certificate',      icon: 'bi-file-earmark-person'  },
  { key: 'NtmiMedical',      label: 'NTMI Medical Certificate', icon: 'bi-file-earmark-medical' },
  { key: 'NicCopy',          label: 'NIC Copy',                icon: 'bi-card-text'            }
];

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
  billsError = '';
  activeTab: 'info' | 'payments' | 'docs' = 'info';
  private apiUrl = 'http://localhost:5062/api';

  // ── Documents ─────────────────────────────────────────────────────────────
  readonly docSlotDefs = DOC_SLOTS;
  uploadedDocs: any[] = [];    // from API
  isDocsLoading = false;

  // per-slot upload state
  uploadState: Record<string, { file: File | null; preview: string | null; uploading: boolean; error: string }> = {
    BirthCertificate: { file: null, preview: null, uploading: false, error: '' },
    NtmiMedical:      { file: null, preview: null, uploading: false, error: '' },
    NicCopy:          { file: null, preview: null, uploading: false, error: '' }
  };

  deleteConfirmId: number | null = null;
  deleteBillConfirmId: number | null = null;
  isDeletingBill = false;


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
  get studentId() { return this.student?.id; }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadStudent(id: number) {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.student = data;
        this.isLoading = false;
        this.loadDocs(id);
      },
      error: () => { this.errorMessage = 'Student not found.'; this.isLoading = false; }
    });
  }

  loadBills(id: number) {
    this.isBillsLoading = true;
    this.billsError = '';
    this.http.get<any[]>(`${this.apiUrl}/billing/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.bills = data;
        this.filteredBills = data;
        this.isBillsLoading = false;
      },
      error: (err) => {
        const msg   = err.error?.message || err.statusText || 'Failed to load bills';
        const inner = err.error?.inner   ? ` — ${err.error.inner}` : '';
        this.billsError = `Error ${err.status}: ${msg}${inner}`;
        this.isBillsLoading = false;
      }
    });
  }

  loadDocs(id: number) {
    this.isDocsLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/student-document/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.uploadedDocs = data; this.isDocsLoading = false; },
      error: () => { this.isDocsLoading = false; }
    });
  }

  // ── Get latest doc for a type ─────────────────────────────────────────────
  docFor(type: string) {
    return this.uploadedDocs.find(d => d.documentType === type) ?? null;
  }

  isImage(doc: any) {
    return doc?.contentType?.startsWith('image/');
  }

  viewDoc(doc: any) {
    const url = `${this.apiUrl}/student-document/file/${doc.id}`;
    this.http.get(url, { headers: this.getHeaders(), responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objUrl = URL.createObjectURL(blob);
        window.open(objUrl, '_blank');
      }
    });
  }

  confirmDelete(id: number) { this.deleteConfirmId = id; }
  cancelDelete()            { this.deleteConfirmId = null; }

  doDeleteDoc() {
    if (!this.deleteConfirmId) return;
    const id = this.deleteConfirmId;
    this.deleteConfirmId = null;
    this.http.delete(`${this.apiUrl}/student-document/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => { this.loadDocs(this.studentId); this.loadStudent(this.studentId); },
      error: () => {}
    });
  }

  // ── File picking + upload ─────────────────────────────────────────────────
  onFileSelected(event: Event, type: string) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const state = this.uploadState[type];
    if (!file || !state) return;

    state.error = '';
    const allowed = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp', 'application/pdf'];
    if (!allowed.includes(file.type)) { state.error = 'Only JPG, PNG, WebP, or PDF allowed.'; return; }
    if (file.size > 10 * 1024 * 1024)  { state.error = 'File must be under 10 MB.'; return; }

    state.file = file;
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = (e) => { state.preview = e.target?.result as string; };
      reader.readAsDataURL(file);
    } else {
      state.preview = null;
    }
  }

  uploadDoc(type: string) {
    const state = this.uploadState[type];
    if (!state?.file || !this.studentId) return;

    state.uploading = true;
    state.error = '';

    const formData = new FormData();
    formData.append('studentId', this.studentId.toString());
    formData.append('documentType', type);
    formData.append('file', state.file);

    this.http.post(`${this.apiUrl}/student-document/upload`, formData, {
      headers: new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` })
    }).subscribe({
      next: () => {
        state.uploading = false;
        state.file = null;
        state.preview = null;
        this.loadDocs(this.studentId);
        this.loadStudent(this.studentId);
      },
      error: (err) => {
        state.uploading = false;
        state.error = err.error?.message || 'Upload failed.';
      }
    });
  }

  clearUpload(type: string) {
    const state = this.uploadState[type];
    if (state) { state.file = null; state.preview = null; state.error = ''; }
  }

  // ── Package helper ────────────────────────────────────────────────────────
  codesOf(pkg: any): string[] {
    return (pkg?.vehicleClassCodes as string ?? '').split(',').map((c: string) => c.trim()).filter(Boolean);
  }

  // ── Bills ─────────────────────────────────────────────────────────────────
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

  // Package price is the fixed total — never multiply across bills
  get totalBilled()  { return this.student?.coursePackage?.price || 0; }
  get totalPaid()    { return this.bills.reduce((s, b) => s + (b.paidAmount || 0), 0); }
  get totalPending() { return Math.max(0, this.totalBilled - this.totalPaid); }

  // ── Delete Bill ───────────────────────────────────────────────────────────
  confirmDeleteBill(billId: number) { this.deleteBillConfirmId = billId; }
  cancelDeleteBill()                { this.deleteBillConfirmId = null; }

  doDeleteBill() {
    if (!this.deleteBillConfirmId) return;
    const id = this.deleteBillConfirmId;
    this.deleteBillConfirmId = null;
    this.isDeletingBill = true;
    this.http.delete(`${this.apiUrl}/billing/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => { this.isDeletingBill = false; this.loadBills(this.studentId); },
      error: () => { this.isDeletingBill = false; }
    });
  }

  // ── Record new instalment → goes to billing form with student pre-selected ─
  recordInstalment() {
    this.router.navigate(['/billing/new'], { queryParams: { studentId: this.studentId } });
  }

  editStudent()   { this.router.navigate(['/students/edit', this.student.id]); }
  goBack()        { this.router.navigate(['/students']); }

  previewReceipt(billId: number, billNumber?: string) {
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
        // Inject auto-print script so the browser immediately opens Save as PDF dialog
        const withAutoPrint = html.replace('</body>',
          `<script>window.addEventListener('load', function() { setTimeout(function() { window.print(); }, 300); });</script></body>`);
        const w = window.open('', '_blank');
        if (w) { w.document.write(withAutoPrint); w.document.close(); }
      }
    });
  }

  createBill() {
    this.router.navigate(['/billing/new'], { queryParams: { studentId: this.student?.id } });
  }
}
