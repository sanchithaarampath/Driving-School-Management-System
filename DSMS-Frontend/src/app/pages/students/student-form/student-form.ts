import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

const VEHICLE_CLASSES = [
  { code: 'A1', label: 'A1 - Light Bicycle' },
  { code: 'A', label: 'A - Bicycle' },
  { code: 'B1', label: 'B1 - Three Wheeler' },
  { code: 'B_Auto', label: 'B (Auto) - Dual Purpose Auto Gear' },
  { code: 'B_Manual', label: 'B (Manual) - Dual Purpose Manual' },
  { code: 'C1', label: 'C1 - Light Lorry' },
  { code: 'C', label: 'C - Lorry' },
  { code: 'CE', label: 'CE - Prime Mover' },
  { code: 'D1', label: 'D1 - Light Bus' },
  { code: 'D', label: 'D - Bus' },
  { code: 'G', label: 'G - Tractor with Trailer' },
  { code: 'J', label: 'J - Special Vehicles' },
];

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
  vehicleClassOptions = VEHICLE_CLASSES;
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
    vehicleClasses: [] as string[]
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.user = this.authService.getUser();
    this.loadBranches();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.studentId = +id;
      this.loadStudent(this.studentId);
    }
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  loadStudent(id: number) {
    this.isLoading = true;
    this.http.get(`${this.apiUrl}/student/${id}`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => {
        this.student = {
          branchId: data.branchId,
          studentName: data.studentName,
          email: data.email || '',
          phoneNumber: data.phoneNumber,
          whatsAppNumber: data.whatsAppNumber || '',
          address: data.address,
          nic: data.nic,
          dob: data.dob ? data.dob.split('T')[0] : '',
          gender: data.gender || '',
          nearestPoliceStation: data.nearestPoliceStation || '',
          nearestDivisionalSecretariat: data.nearestDivisionalSecretariat || '',
          postalCode: data.postalCode || '',
          existingLicenseNo: data.existingLicenseNo || '',
          packageType: data.packageType || '',
          isSpecialRequirements: data.isSpecialRequirements || false,
          specialRequirementTypeId: data.specialRequirementTypeId || null,
          hasBirthCertificate: data.hasBirthCertificate || false,
          hasNtmiMedical: data.hasNtmiMedical || false,
          hasNicCopy: data.hasNicCopy || false,
          vehicleClasses: data.vehicleClasses || []
        };
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  toggleVehicleClass(code: string) {
    const idx = this.student.vehicleClasses.indexOf(code);
    if (idx === -1) this.student.vehicleClasses.push(code);
    else this.student.vehicleClasses.splice(idx, 1);
  }

  isClassSelected(code: string): boolean {
    return this.student.vehicleClasses.includes(code);
  }

  onSubmit() {
    if (!this.student.studentName || !this.student.nic || !this.student.phoneNumber || !this.student.address) {
      this.errorMessage = 'Please fill all required fields';
      return;
    }
    if (!this.isEditMode && !this.student.dob) {
      this.errorMessage = 'Please enter date of birth';
      return;
    }
    this.isSaving = true;
    this.errorMessage = '';

    const request = this.isEditMode
      ? this.http.put(`${this.apiUrl}/student/${this.studentId}`, this.student, { headers: this.getHeaders() })
      : this.http.post(`${this.apiUrl}/student`, this.student, { headers: this.getHeaders() });

    request.subscribe({
      next: () => {
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

  goBack() { this.router.navigate(['/students']); }
}
