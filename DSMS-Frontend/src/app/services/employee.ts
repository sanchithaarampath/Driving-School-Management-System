import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private apiUrl = 'http://localhost:5062/api';

  constructor(private http: HttpClient, private authService: AuthService) {}

  private getHeaders() {
    return new HttpHeaders({ Authorization: `Bearer ${this.authService.getToken()}` });
  }

  getAll() {
    return this.http.get(`${this.apiUrl}/employee`, { headers: this.getHeaders() });
  }

  getById(id: number) {
    return this.http.get(`${this.apiUrl}/employee/${id}`, { headers: this.getHeaders() });
  }

  create(data: any) {
    return this.http.post(`${this.apiUrl}/employee`, data, { headers: this.getHeaders() });
  }

  update(id: number, data: any) {
    return this.http.put(`${this.apiUrl}/employee/${id}`, data, { headers: this.getHeaders() });
  }

  delete(id: number) {
    return this.http.delete(`${this.apiUrl}/employee/${id}`, { headers: this.getHeaders() });
  }
}
