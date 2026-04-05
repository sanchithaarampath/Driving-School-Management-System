import os

login_ts = '''import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  username = '';
  password = '';
  errorMessage = '';
  isLoading = false;
  showPassword = false;

  constructor(private authService: AuthService, private router: Router) {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onLogin() {
    if (!this.username || !this.password) {
      this.errorMessage = 'Please enter username and password';
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    this.authService.login(this.username, this.password).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Invalid username or password';
      }
    });
  }
}
'''

login_html = '''<div class="login-wrapper">
  <div class="login-card">
    <div class="login-logo">
      <div class="logo-circle">
        <i class="bi bi-car-front-fill"></i>
      </div>
      <h2>Arampath Driving School</h2>
      <p>Driving School Management System</p>
    </div>
    <div class="login-form">
      <h4>Welcome Back</h4>
      <p class="subtitle">Sign in to your account</p>
      <div *ngIf="errorMessage" class="alert alert-danger">
        <i class="bi bi-exclamation-circle me-2"></i>{{ errorMessage }}
      </div>
      <div class="mb-3">
        <label class="form-label">Username</label>
        <div class="input-group">
          <span class="input-group-text"><i class="bi bi-person"></i></span>
          <input type="text" class="form-control" placeholder="Enter username"
            [(ngModel)]="username" (keyup.enter)="onLogin()">
        </div>
      </div>
      <div class="mb-4">
        <label class="form-label">Password</label>
        <div class="input-group">
          <span class="input-group-text"><i class="bi bi-lock"></i></span>
          <input [type]="showPassword ? 'text' : 'password'" class="form-control"
            placeholder="Enter password" [(ngModel)]="password" (keyup.enter)="onLogin()">
          <button class="input-group-text btn-toggle" (click)="showPassword = !showPassword">
            <i class="bi" [ngClass]="showPassword ? 'bi-eye-slash' : 'bi-eye'"></i>
          </button>
        </div>
      </div>
      <button class="btn btn-primary w-100 btn-login" (click)="onLogin()" [disabled]="isLoading">
        <span *ngIf="isLoading" class="spinner-border spinner-border-sm me-2"></span>
        <i *ngIf="!isLoading" class="bi bi-box-arrow-in-right me-2"></i>
        {{ isLoading ? 'Signing in...' : 'Sign In' }}
      </button>
    </div>
    <div class="login-footer">
      <p>DSMS v1 &copy; 2026 Arampath Driving School</p>
    </div>
  </div>
</div>
'''

login_scss = '''.login-wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d1117 0%, #161b22 100%);
  padding: 1rem;
}
.login-card {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 16px;
  width: 100%;
  max-width: 420px;
  padding: 2.5rem;
  box-shadow: 0 20px 60px rgba(0,0,0,0.5);
}
.login-logo {
  text-align: center;
  margin-bottom: 2rem;
}
.logo-circle {
  width: 70px;
  height: 70px;
  background: linear-gradient(135deg, #e63946, #c1121f);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1rem;
  font-size: 1.8rem;
  color: white;
}
.login-logo h2 {
  color: #e6edf3;
  font-size: 1.3rem;
  font-weight: 700;
  margin-bottom: 0.3rem;
}
.login-logo p {
  color: #8b949e;
  font-size: 0.85rem;
}
.login-form h4 { color: #e6edf3; font-weight: 600; margin-bottom: 0.3rem; }
.subtitle { color: #8b949e; font-size: 0.9rem; margin-bottom: 1.5rem; }
.form-label { color: #8b949e; font-size: 0.85rem; font-weight: 500; }
.input-group-text { background: #0d1117; border-color: #30363d; color: #8b949e; }
.btn-toggle { cursor: pointer; }
.btn-login { padding: 0.7rem; font-weight: 600; border-radius: 8px; font-size: 0.95rem; }
.login-footer { text-align: center; margin-top: 2rem; }
.login-footer p { color: #8b949e; font-size: 0.8rem; }
.alert-danger { background: rgba(230,57,70,0.1); border-color: rgba(230,57,70,0.3); color: #ff6b7a; font-size: 0.875rem; }
'''

os.makedirs("src/app/pages/login", exist_ok=True)
with open("src/app/pages/login/login.ts", "w", encoding="utf-8") as f:
    f.write(login_ts)
with open("src/app/pages/login/login.html", "w", encoding="utf-8") as f:
    f.write(login_html)
with open("src/app/pages/login/login.scss", "w", encoding="utf-8") as f:
    f.write(login_scss)
print("Login page created successfully!")
