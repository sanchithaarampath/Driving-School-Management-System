import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';


interface DocSlot {
  key: 'BirthCertificate' | 'NtmiMedical' | 'NicCopy';
  label: string;
  icon: string;
  file: File | null;
  preview: string | null;
  uploading: boolean;
  uploaded: boolean;
  error: string;
}

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './student-form.html',
  styleUrl: './student-form.scss'
})
export class StudentForm implements OnInit {
  isEditMode = false;
  studentId: number | null = null;
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  user: any;
  branches: any[] = [];
  private apiUrl = 'http://localhost:5062/api';

  student = {
    branchId: 1,
    studentName: '',
    email: '',
    phoneNumber: '',
    whatsAppNumber: '',
    address: '',
    nic: '',
    dob: '',
    gender: '',
    nearestPoliceStation: '',
    nearestDivisionalSecretariat: '',
    postalCode: '',
    existingLicenseNo: '',
    packageType: '',
    isSpecialRequirements: false,
    specialRequirementTypeId: null as number | null,
    hasBirthCertificate: false,
    hasNtmiMedical: false,
    hasNicCopy: false,
    coursePackageId: null as number | null,
    vehicleClasses: [] as string[]
  };

  // ── Course Package Picker ─────────────────────────────────────────────────
  allPackages: any[] = [];
  selectedPackage: any = null;
  pkgSearch = '';

  get filteredPackages() {
    const term = this.pkgSearch.trim().toLowerCase();
    if (!term) return this.allPackages;
    return this.allPackages.filter(p =>
      p.packageName?.toLowerCase().includes(term) ||
      p.courseType?.toLowerCase().includes(term) ||
      p.vehicleClassCodes?.toLowerCase().includes(term)
    );
  }

  codesOf(pkg: any): string[] {
    return (pkg.vehicleClassCodes as string).split(',').map((c: string) => c.trim()).filter(Boolean);
  }

  selectPackage(pkg: any) {
    this.selectedPackage         = pkg;
    this.student.coursePackageId = pkg.id;

    // Auto-fill course type from package
    const ct = pkg.courseType as string;
    if (ct?.includes('Full'))      this.student.packageType = 'FullCoursework';
    else if (ct?.includes('Semi')) this.student.packageType = 'SemiCoursework';
    else                           this.student.packageType = ct ?? '';

    // Vehicle classes come entirely from the package
    this.student.vehicleClasses = this.codesOf(pkg);
  }

  clearPackage() {
    this.selectedPackage         = null;
    this.student.coursePackageId = null;
    this.student.vehicleClasses  = [];
    this.student.packageType     = '';
  }

  // ── Document Upload Slots ─────────────────────────────────────────────────
  docSlots: DocSlot[] = [
    { key: 'BirthCertificate', label: 'Birth Certificate',       icon: 'bi-file-earmark-person',  file: null, preview: null, uploading: false, uploaded: false, error: '' },
    { key: 'NtmiMedical',      label: 'NTMI Medical Certificate', icon: 'bi-file-earmark-medical', file: null, preview: null, uploading: false, uploaded: false, error: '' },
    { key: 'NicCopy',          label: 'NIC Copy',                 icon: 'bi-card-text',            file: null, preview: null, uploading: false, uploaded: false, error: '' }
  ];
  savedStudentId: number | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  get isCompanyAdmin() { return this.authService.isCompanyAdmin(); }
  get isBranchLocked() { return !this.authService.isCompanyAdmin(); } // Staff & Branch Admin are locked to their branch
  get myBranchId(): number | null { return this.authService.getBranchId(); }
  get myBranchName(): string {
    const b = this.branches.find(x => x.id === this.myBranchId);
    return b ? b.name : 'My Branch';
  }

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadBranches();
    this.loadPackages();

    // Pre-set branch to user's own branch if not Company Admin
    if (!this.isCompanyAdmin && this.myBranchId) {
      this.student.branchId = this.myBranchId;
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.studentId = +id;
      this.savedStudentId = +id;
      this.loadStudent(this.studentId);
    }
  }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` }); }

  loadBranches() {
    this.http.get(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  loadPackages() {
    this.http.get<any[]>(`${this.apiUrl}/course-package`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.allPackages = data; }
    });
  }

  loadStudent(id: number) {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        this.student = {
          branchId:                    data.branchId,
          studentName:                 data.studentName,
          email:                       data.email || '',
          phoneNumber:                 data.phoneNumber,
          whatsAppNumber:              data.whatsAppNumber || '',
          address:                     data.address,
          nic:                         data.nic,
          dob:                         data.dob ? data.dob.split('T')[0] : '',
          gender:                      data.gender || '',
          nearestPoliceStation:        data.nearestPoliceStation || '',
          nearestDivisionalSecretariat: data.nearestDivisionalSecretariat || '',
          postalCode:                  data.postalCode || '',
          existingLicenseNo:           data.existingLicenseNo || '',
          packageType:                 data.packageType || '',
          isSpecialRequirements:       data.isSpecialRequirements || false,
          specialRequirementTypeId:    data.specialRequirementTypeId || null,
          hasBirthCertificate:         data.hasBirthCertificate || false,
          hasNtmiMedical:              data.hasNtmiMedical || false,
          hasNicCopy:                  data.hasNicCopy || false,
          coursePackageId:             data.coursePackageId || null,
          vehicleClasses:              data.vehicleClasses || []
        };

        // Restore selected package object if it exists
        if (data.coursePackage) this.selectedPackage = data.coursePackage;

        this.docSlots[0].uploaded = data.hasBirthCertificate || false;
        this.docSlots[1].uploaded = data.hasNtmiMedical || false;
        this.docSlots[2].uploaded = data.hasNicCopy || false;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  // ── Document file picking ─────────────────────────────────────────────────
  onFileSelected(event: Event, slot: DocSlot) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    slot.error = '';
    const allowed = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp', 'application/pdf'];
    if (!allowed.includes(file.type)) { slot.error = 'Only JPG, PNG, WebP, or PDF allowed.'; return; }
    if (file.size > 10 * 1024 * 1024) { slot.error = 'File must be under 10 MB.'; return; }
    slot.file = file;
    slot.uploaded = false;
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = (e) => { slot.preview = e.target?.result as string; };
      reader.readAsDataURL(file);
    } else { slot.preview = null; }
  }

  clearFile(slot: DocSlot) { slot.file = null; slot.preview = null; slot.uploaded = false; slot.error = ''; }

  private uploadDoc(slot: DocSlot, studentId: number): Promise<void> {
    if (!slot.file) return Promise.resolve();
    slot.uploading = true; slot.error = '';
    const formData = new FormData();
    formData.append('studentId', studentId.toString());
    formData.append('documentType', slot.key);
    formData.append('file', slot.file);
    return new Promise((resolve) => {
      this.http.post(`${this.apiUrl}/student-document/upload`, formData, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` })
      }).subscribe({
        next: () => { slot.uploading = false; slot.uploaded = true; slot.file = null; resolve(); },
        error: (err) => { slot.uploading = false; slot.error = err.error?.message || 'Upload failed.'; resolve(); }
      });
    });
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  async onSubmit() {
    if (!this.student.studentName || !this.student.nic || !this.student.phoneNumber || !this.student.address) {
      this.errorMessage = 'Please fill all required fields (Name, NIC, Phone, Address).'; return;
    }
    if (!this.isEditMode && !this.student.dob) {
      this.errorMessage = 'Please enter date of birth.'; return;
    }
    if (!this.student.coursePackageId) {
      this.errorMessage = 'Please select a Course Package before registering.'; return;
    }
    this.isSaving = true; this.errorMessage = '';

    const request = this.isEditMode
      ? this.http.put<any>(`${this.apiUrl}/student/${this.studentId}`, this.student, { headers: this.getHeaders() })
      : this.http.post<any>(`${this.apiUrl}/student`, this.student, { headers: this.getHeaders() });

    request.subscribe({
      next: async (res: any) => {
        const sid = this.isEditMode ? this.studentId! : (res.id ?? res.studentId ?? this.savedStudentId!);
        this.savedStudentId = sid;
        const uploads = this.docSlots.filter(s => s.file !== null);
        for (const slot of uploads) await this.uploadDoc(slot, sid);
        this.isSaving = false;
        this.successMessage = this.isEditMode ? 'Student updated successfully!' : 'Student registered successfully!';
        setTimeout(() => this.router.navigate(['/students']), 1500);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'An error occurred. Please try again.';
      }
    });
  }

  get anyUploading() { return this.docSlots.some(s => s.uploading); }
  goBack() { this.router.navigate(['/students']); }
}
