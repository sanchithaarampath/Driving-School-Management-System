import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = 'http://localhost:5062/api/auth';

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, { userName: username, password }).pipe(
      tap((response: any) => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify({
          userId: response.userId,
          userFullName: response.userFullName,
          userName: response.userName,
          role: response.role,
          roleId: response.roleId,
          firstTimeLogin: response.firstTimeLogin
        }));
      })
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.router.navigate(['/login']);
  }

  getToken(): string | null { return localStorage.getItem('token'); }
  getUser(): any { const u = localStorage.getItem('user'); return u ? JSON.parse(u) : null; }
  isLoggedIn(): boolean { return !!this.getToken(); }
  isAdmin(): boolean { return this.getUser()?.role === 'Admin'; }

  changePassword(userId: number, currentPassword: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, { userId, currentPassword, newPassword });
  }
}