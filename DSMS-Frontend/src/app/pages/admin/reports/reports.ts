import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './reports.html',
  styleUrl: './reports.scss'
})
export class ReportsPage implements OnInit {

  activeTab: 'income' | 'students' | 'exam' = 'income';

  // Date range
  fromDate = this.firstDayOfMonth();
  toDate   = new Date().toISOString().split('T')[0];

  // Income report
  incomeData:   any = null;
  isLoading     = false;
  errorMessage  = '';

  // Students report
  studentsData: any = null;

  // Exam summary
  examData: any = null;

  private apiUrl = 'http://localhost:5062/api';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() { this.loadIncome(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  firstDayOfMonth() {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().split('T')[0];
  }

  switchTab(tab: 'income' | 'students' | 'exam') {
    this.activeTab = tab;
    if (tab === 'income'   && !this.incomeData)  this.loadIncome();
    if (tab === 'students' && !this.studentsData) this.loadStudents();
    if (tab === 'exam'     && !this.examData)     this.loadExam();
  }

  loadIncome() {
    this.isLoading   = true;
    this.errorMessage = '';
    this.http.get<any>(`${this.apiUrl}/reports/income?from=${this.fromDate}&to=${this.toDate}`,
      { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.incomeData = data; this.isLoading = false; },
      error: (err)  => { this.errorMessage = err.error?.message || 'Failed to load report.'; this.isLoading = false; }
    });
  }

  loadStudents() {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/reports/students?from=${this.fromDate}&to=${this.toDate}`,
      { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.studentsData = data; this.isLoading = false; },
      error: ()     => { this.isLoading = false; }
    });
  }

  loadExam() {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/reports/exam-summary`, { headers: this.getHeaders() }).subscribe({
      next: (data) => { this.examData = data; this.isLoading = false; },
      error: ()     => { this.isLoading = false; }
    });
  }

  applyDateRange() {
    this.incomeData   = null;
    this.studentsData = null;
    if (this.activeTab === 'income')   this.loadIncome();
    if (this.activeTab === 'students') this.loadStudents();
  }

  setRange(preset: string) {
    const today = new Date();
    if (preset === 'today') {
      this.fromDate = this.toDate = today.toISOString().split('T')[0];
    } else if (preset === 'week') {
      const mon = new Date(today);
      mon.setDate(today.getDate() - today.getDay() + 1);
      this.fromDate = mon.toISOString().split('T')[0];
      this.toDate   = today.toISOString().split('T')[0];
    } else if (preset === 'month') {
      this.fromDate = this.firstDayOfMonth();
      this.toDate   = today.toISOString().split('T')[0];
    } else if (preset === 'year') {
      this.fromDate = `${today.getFullYear()}-01-01`;
      this.toDate   = today.toISOString().split('T')[0];
    }
    this.applyDateRange();
  }

  printReport() { window.print(); }

  barWidth(value: number, max: number): number {
    if (!max) return 0;
    return Math.round((value / max) * 100);
  }

  get maxDailyPaid(): number {
    if (!this.incomeData?.daily?.length) return 1;
    return Math.max(...this.incomeData.daily.map((d: any) => d.totalPaid));
  }
}
