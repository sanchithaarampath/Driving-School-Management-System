import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../../services/auth';
import { SidebarComponent } from '../../../shared/layout/sidebar';
import { TopbarComponent } from '../../../shared/layout/topbar';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, SidebarComponent, TopbarComponent],
  templateUrl: './users.html',
  styleUrl: './users.scss'
})
export class UsersPage implements OnInit {
  users: any[] = [];
  roles: any[] = [];
  branches: any[] = [];
  isLoading = true;
  showForm = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';
  private apiUrl = 'http://localhost:5062/api';

  get isCompanyAdmin() { return this.auth.isCompanyAdmin(); }
  get isBranchAdmin()  { return this.auth.isBranchAdmin(); }
  get myBranchId()     { return this.auth.getBranchId(); }
  get myBranchName(): string {
    const bid = this.myBranchId;
    if (!bid) return '';
    const b = this.branches.find(b => b.id === bid);
    return b ? b.name : `Branch #${bid}`;
  }

  // ── Create user form ──────────────────────────────────────────────────────
  form = { userName: '', password: '', userFullName: '', roleId: 0, branchId: null as number | null };
  showCreatePassword = false;

  // ── Edit user modal ───────────────────────────────────────────────────────
  showEditModal = false;
  editTarget: any = null;
  isEditSaving  = false;
  editError     = '';
  editForm = { userFullName: '', roleId: 0, branchId: null as number | null, active: true };

  // ── Permanent delete modal ────────────────────────────────────────────────
  showDeleteModal  = false;
  deleteTarget: any = null;
  isDeleting       = false;
  deleteError      = '';

  // ── Set / Reset password modal ────────────────────────────────────────────
  showPasswordModal  = false;
  passwordTarget: any = null;
  pwForm = { newPassword: '', confirmPassword: '' };
  pwShowNew     = false;
  pwShowConfirm = false;
  pwSaving      = false;
  pwError       = '';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() { this.loadUsers(); this.loadRoles(); this.loadBranches(); }

  getHeaders() { return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }); }

  loadUsers() {
    this.http.get(`${this.apiUrl}/users`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.users = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  loadRoles() {
    this.http.get(`${this.apiUrl}/users/roles`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.roles = data; }
    });
  }

  loadBranches() {
    this.http.get(`${this.apiUrl}/lookup/branches`, { headers: this.getHeaders() }).subscribe({
      next: (data: any) => { this.branches = data; }
    });
  }

  // ── Create user ───────────────────────────────────────────────────────────
  openForm() {
    this.showForm = true;
    this.showCreatePassword = false;
    this.form = { userName: '', password: '', userFullName: '', roleId: 0, branchId: null };
    this.errorMessage = '';
  }
  cancel() { this.showForm = false; this.errorMessage = ''; }

  save() {
    this.errorMessage = '';
    if (!this.form.userFullName.trim()) { this.errorMessage = 'Full name is required.'; return; }
    if (!this.form.userName.trim())     { this.errorMessage = 'Username is required.'; return; }
    if (!this.form.password || this.form.password.length < 6) { this.errorMessage = 'Password must be at least 6 characters.'; return; }
    if (!this.form.roleId)              { this.errorMessage = 'Please select a role.'; return; }

    this.isSaving = true;
    this.http.post(`${this.apiUrl}/users`, this.form, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;
        this.successMessage = 'User created successfully!';
        this.loadUsers();
        setTimeout(() => { this.successMessage = ''; }, 3500);
      },
      error: (err: any) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'Failed to create user.';
      }
    });
  }

  // ── Edit modal ────────────────────────────────────────────────────────────
  openEdit(user: any) {
    this.editTarget = user;
    this.editForm = {
      userFullName: user.userFullName,
      roleId:       user.roleId,
      branchId:     user.branchId ?? null,
      active:       user.active
    };
    this.editError    = '';
    this.showEditModal = true;
  }

  closeEdit() { this.showEditModal = false; this.editTarget = null; }

  // ── Quick reactivate (one-click for inactive users) ───────────────────────
  quickReactivate(user: any) {
    const payload = {
      userFullName: user.userFullName,
      roleId:       user.roleId,
      branchId:     user.branchId ?? null,
      active:       true
    };
    this.http.put(`${this.apiUrl}/users/${user.id}`, payload, { headers: this.getHeaders() }).subscribe({
      next: () => {
        user.active = true;
        this.successMessage = `${user.userFullName} has been reactivated.`;
        setTimeout(() => { this.successMessage = ''; }, 3500);
      },
      error: (err: any) => {
        this.errorMessage = err.error?.message || 'Failed to reactivate user.';
        setTimeout(() => { this.errorMessage = ''; }, 4000);
      }
    });
  }

  saveEdit() {
    this.editError = '';
    if (!this.editForm.userFullName.trim()) { this.editError = 'Full name is required.'; return; }
    if (!this.editForm.roleId)              { this.editError = 'Please select a role.'; return; }

    this.isEditSaving = true;
    this.http.put(
      `${this.apiUrl}/users/${this.editTarget.id}`,
      this.editForm,
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.isEditSaving = false;
        this.showEditModal = false;
        this.successMessage = `${this.editTarget.userFullName} updated successfully.`;
        this.editTarget = null;
        this.loadUsers();
        setTimeout(() => { this.successMessage = ''; }, 3500);
      },
      error: (err: any) => {
        this.isEditSaving = false;
        this.editError = err.error?.message || 'Failed to update user.';
      }
    });
  }

  // ── Permanent delete modal ────────────────────────────────────────────────
  openDeleteModal(user: any) {
    this.deleteTarget = user;
    this.deleteError  = '';
    this.showDeleteModal = true;
  }
  closeDeleteModal() { this.showDeleteModal = false; this.deleteTarget = null; }

  doDelete() {
    if (!this.deleteTarget) return;
    const id   = this.deleteTarget.id;
    const name = this.deleteTarget.userFullName;
    this.isDeleting  = true;
    this.deleteError = '';
    this.http.delete(`${this.apiUrl}/users/${id}`, { headers: this.getHeaders() }).subscribe({
      next: () => {
        this.isDeleting      = false;
        this.showDeleteModal = false;
        this.deleteTarget    = null;
        this.users           = this.users.filter(u => u.id !== id);
        this.successMessage  = `${name} has been permanently deleted.`;
        setTimeout(() => { this.successMessage = ''; }, 4000);
      },
      error: (err: any) => {
        this.isDeleting  = false;
        this.deleteError = err.error?.message || 'Failed to delete user. Please try again.';
      }
    });
  }

  // ── Password modal ────────────────────────────────────────────────────────
  openPasswordModal(user: any) {
    this.passwordTarget = user;
    this.pwForm = { newPassword: '', confirmPassword: '' };
    this.pwShowNew = false;
    this.pwShowConfirm = false;
    this.pwError = '';
    this.pwSaving = false;
    this.showPasswordModal = true;
  }

  closePasswordModal() {
    this.showPasswordModal = false;
    this.passwordTarget = null;
  }

  savePassword() {
    this.pwError = '';
    if (!this.pwForm.newPassword || this.pwForm.newPassword.length < 6) {
      this.pwError = 'Password must be at least 6 characters.';
      return;
    }
    if (this.pwForm.newPassword !== this.pwForm.confirmPassword) {
      this.pwError = 'Passwords do not match.';
      return;
    }

    this.pwSaving = true;
    this.http.post(
      `${this.apiUrl}/users/${this.passwordTarget.id}/reset-password`,
      { newPassword: this.pwForm.newPassword },
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.pwSaving = false;
        this.showPasswordModal = false;
        this.successMessage = `Password updated for ${this.passwordTarget.userFullName}.`;
        this.passwordTarget = null;
        setTimeout(() => { this.successMessage = ''; }, 4000);
      },
      error: (err: any) => {
        this.pwSaving = false;
        this.pwError = err.error?.message || 'Failed to update password.';
      }
    });
  }

  // ── Branch name lookup ────────────────────────────────────────────────────
  getBranchName(branchId: number | null): string {
    if (!branchId) return 'All Branches';
    const b = this.branches.find(b => b.id === branchId);
    return b ? b.name : `Branch #${branchId}`;
  }

  // ── Role badge CSS class ──────────────────────────────────────────────────
  getRoleBadgeClass(roleName: string) {
    switch (roleName) {
      case 'Company Admin': return 'role-badge company-admin';
      case 'Admin':         return 'role-badge company-admin';
      case 'Branch Admin':  return 'role-badge branch-admin';
      case 'Staff':         return 'role-badge staff';
      case 'Instructor':    return 'role-badge instructor';
      default:              return 'role-badge staff';
    }
  }
}
