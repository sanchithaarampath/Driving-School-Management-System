import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './settings.html',
  styleUrl: './settings.scss'
})
export class SettingsPage implements OnInit {
  private apiUrl = 'http://localhost:5062/api';
  isLoading = true;
  isSaving  = false;
  saveMsg   = '';
  saveError = '';

  // Email
  email = { smtpHost: '', smtpPort: '587', senderEmail: '', senderName: '', appPassword: '', configured: false };
  emailStatus = { configured: false };
  testEmail = '';
  testEmailMsg = ''; testEmailErr = ''; testEmailSending = false;
  showEmailPass = false;

  // Twilio / WhatsApp
  twilio = { accountSid: '', authToken: '', whatsAppFrom: 'whatsapp:+14155238886', configured: false };
  testPhone = '';
  testWaMsg = ''; testWaErr = ''; testWaSending = false;
  showTwilioToken = false;

  // School Info
  school = { name: '', address: '', phone: '', email: '' };

  // Audit Log
  auditLog: any[] = [];
  isAuditLoading = false;

  // Overpaid analysis
  overpaid: any[] = [];
  isOverpaidLoading = false;
  cleanupMsg = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.loadSettings();
    this.loadAuditLog();
    this.loadOverpaidAnalysis();
  }

  getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  loadSettings() {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/system-settings`, { headers: this.getHeaders() }).subscribe({
      next: (data) => {
        this.email  = { ...this.email,  ...data.email  };
        this.twilio = { ...this.twilio, ...data.twilio };
        this.school = { ...this.school, ...data.school };
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  saveSettings() {
    this.isSaving = true; this.saveMsg = ''; this.saveError = '';
    const payload = {
      email:  { smtpHost: this.email.smtpHost, smtpPort: this.email.smtpPort,
                senderEmail: this.email.senderEmail, senderName: this.email.senderName,
                appPassword: this.email.appPassword },
      twilio: { accountSid: this.twilio.accountSid, authToken: this.twilio.authToken,
                whatsAppFrom: this.twilio.whatsAppFrom },
      school: { ...this.school }
    };
    this.http.put<any>(`${this.apiUrl}/system-settings`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => { this.isSaving = false; this.saveMsg = 'Settings saved successfully!'; this.loadSettings(); },
      error: (err) => { this.isSaving = false; this.saveError = err.error?.message || 'Failed to save.'; }
    });
  }

  sendTestEmail() {
    this.testEmailSending = true; this.testEmailMsg = ''; this.testEmailErr = '';
    this.http.post<any>(`${this.apiUrl}/system-settings/test-email`,
      { toEmail: this.testEmail }, { headers: this.getHeaders() }).subscribe({
      next: (r) => { this.testEmailSending = false; this.testEmailMsg = r.message; },
      error: (e) => { this.testEmailSending = false; this.testEmailErr = e.error?.message || 'Failed.'; }
    });
  }

  sendTestWhatsApp() {
    this.testWaSending = true; this.testWaMsg = ''; this.testWaErr = '';
    this.http.post<any>(`${this.apiUrl}/system-settings/test-whatsapp`,
      { toPhone: this.testPhone }, { headers: this.getHeaders() }).subscribe({
      next: (r) => { this.testWaSending = false; this.testWaMsg = r.message; },
      error: (e) => { this.testWaSending = false; this.testWaErr = e.error?.message || 'Failed.'; }
    });
  }

  loadAuditLog() {
    this.isAuditLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/billing/audit-log`, { headers: this.getHeaders() }).subscribe({
      next: (d) => { this.auditLog = d; this.isAuditLoading = false; },
      error: () => { this.isAuditLoading = false; }
    });
  }

  loadOverpaidAnalysis() {
    this.isOverpaidLoading = true;
    this.http.get<any[]>(`${this.apiUrl}/billing/overpaid-analysis`, { headers: this.getHeaders() }).subscribe({
      next: (d) => { this.overpaid = d; this.isOverpaidLoading = false; },
      error: () => { this.isOverpaidLoading = false; }
    });
  }

  cleanupStudent(studentId: number, studentName: string) {
    if (!confirm(`Delete ALL bills for ${studentName}? This cannot be undone.`)) return;
    this.http.delete<any>(`${this.apiUrl}/billing/student/${studentId}/all-bills`,
      { headers: this.getHeaders() }).subscribe({
      next: (r) => { this.cleanupMsg = r.message; this.loadOverpaidAnalysis(); },
      error: (e) => { this.cleanupMsg = e.error?.message || 'Delete failed.'; }
    });
  }

  goBack() { this.router.navigate(['/dashboard']); }
}
