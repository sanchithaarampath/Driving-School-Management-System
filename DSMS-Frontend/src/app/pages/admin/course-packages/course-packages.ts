import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

const VEHICLE_CLASS_OPTIONS = [
  { code: 'A1',       label: 'A1 — Light Motorcycle' },
  { code: 'A',        label: 'A — Motorcycle' },
  { code: 'B1',       label: 'B1 — Three Wheeler' },
  { code: 'B_Auto',   label: 'B (Auto) — Dual Purpose Auto' },
  { code: 'B_Manual', label: 'B (Manual) — Dual Purpose Manual' },
  { code: 'C1',       label: 'C1 — Light Lorry' },
  { code: 'C',        label: 'C — Lorry' },
  { code: 'CE',       label: 'CE — Prime Mover' },
  { code: 'D1',       label: 'D1 — Light Bus' },
  { code: 'D',        label: 'D — Bus' },
  { code: 'G1',       label: 'G1 — Auto-added' },
  { code: 'G',        label: 'G — Tractor with Trailer' },
  { code: 'J',        label: 'J — Special Vehicles' }
];

@Component({
  selector: 'app-course-packages',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './course-packages.html',
  styleUrl:    './course-packages.scss'
})
export class CoursePackagesPage implements OnInit {
  packages: any[] = [];
  isLoading = true;
  private apiUrl = 'http://localhost:5062/api';

  readonly vehicleClassOptions = VEHICLE_CLASS_OPTIONS;
  readonly courseTypes = ['Full Course', 'Semi Course', 'Theory Only', 'Practical Only'];

  // ── Modal ─────────────────────────────────────────────────────────────────
  showModal   = false;
  isEditing   = false;
  isSaving    = false;
  saveSuccess = '';
  saveError   = '';

  form = {
    id: 0,
    packageName:       '',
    courseType:        'Full Course',
    selectedCodes:     [] as string[],
    price:             0,
    maxDiscount:       0,
    description:       ''
  };

  // ── Delete confirm ────────────────────────────────────────────────────────
  showDeleteConfirm = false;
  deletingId: number | null = null;
  deletingName = '';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() { this.loadPackages(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadPackages() {
    this.isLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/course-package`, { headers: this.getHeaders() }).subscribe({
      next: (d) => { this.packages = d; this.isLoading = false; },
      error: ()  => { this.isLoading = false; }
    });
  }

  // Vehicle class checkbox helpers
  toggleCode(code: string) {
    const i = this.form.selectedCodes.indexOf(code);
    if (i >= 0) this.form.selectedCodes.splice(i, 1);
    else        this.form.selectedCodes.push(code);
  }

  isCodeSelected(code: string) { return this.form.selectedCodes.includes(code); }

  get selectedCodesLabel() {
    if (!this.form.selectedCodes.length) return 'None selected';
    return this.form.selectedCodes.join(', ');
  }

  codesOf(pkg: any): string[] {
    return (pkg.vehicleClassCodes as string).split(',').map((c: string) => c.trim()).filter(Boolean);
  }

  // ── Open / close modal ────────────────────────────────────────────────────
  openCreate() {
    this.isEditing = false;
    this.form = { id: 0, packageName: '', courseType: 'Full Course', selectedCodes: [], price: 0, maxDiscount: 0, description: '' };
    this.saveSuccess = '';
    this.saveError   = '';
    this.showModal   = true;
  }

  openEdit(pkg: any) {
    this.isEditing = true;
    this.form = {
      id:            pkg.id,
      packageName:   pkg.packageName,
      courseType:    pkg.courseType,
      selectedCodes: this.codesOf(pkg),
      price:         pkg.price,
      maxDiscount:   pkg.maxDiscount,
      description:   pkg.description ?? ''
    };
    this.saveSuccess = '';
    this.saveError   = '';
    this.showModal   = true;
  }

  closeModal() { this.showModal = false; }

  save() {
    if (!this.form.packageName || !this.form.price || !this.form.selectedCodes.length) {
      this.saveError = 'Package name, at least one vehicle class, and price are required.';
      return;
    }
    this.isSaving  = true;
    this.saveError = '';

    const payload = {
      packageName:       this.form.packageName,
      courseType:        this.form.courseType,
      vehicleClassCodes: this.form.selectedCodes.join(','),
      price:             this.form.price,
      maxDiscount:       this.form.maxDiscount,
      description:       this.form.description
    };

    const req = this.isEditing
      ? this.http.put<any>(`${this.apiUrl}/course-package/${this.form.id}`, payload, { headers: this.getHeaders() })
      : this.http.post<any>(`${this.apiUrl}/course-package`, payload, { headers: this.getHeaders() });

    req.subscribe({
      next: () => {
        this.isSaving    = false;
        this.saveSuccess = this.isEditing ? 'Package updated!' : 'Package created!';
        this.loadPackages();
        setTimeout(() => this.closeModal(), 1200);
      },
      error: (err) => {
        this.isSaving  = false;
        this.saveError = err.error?.message || 'Failed to save package.';
      }
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
  confirmDelete(pkg: any) {
    this.deletingId   = pkg.id;
    this.deletingName = pkg.packageName;
    this.showDeleteConfirm = true;
  }

  cancelDelete() { this.showDeleteConfirm = false; this.deletingId = null; }

  doDelete() {
    if (!this.deletingId) return;
    this.http.delete(`${this.apiUrl}/course-package/${this.deletingId}`, { headers: this.getHeaders() }).subscribe({
      next: () => { this.showDeleteConfirm = false; this.deletingId = null; this.loadPackages(); },
      error: ()  => { this.showDeleteConfirm = false; }
    });
  }
}
