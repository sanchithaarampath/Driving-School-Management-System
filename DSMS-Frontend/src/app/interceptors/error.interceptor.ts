import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth';

/**
 * Global HTTP Error Interceptor
 *
 * Catches every failed HTTP request in the entire app and:
 *  - 401 → clears session, redirects to login
 *  - 403 → shows a permission-denied message
 *  - 404 → shows a not-found message
 *  - 422 → passes the server validation message through
 *  - 500 → shows a friendly server-error message
 *  - Network error → shows a connection error
 *
 * The error is re-thrown so individual components can still
 * catch it in their own error handlers if needed.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth   = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Network / CORS / offline ─────────────────────────────────────────
      if (error.status === 0) {
        console.error('[DSMS] Network error — backend unreachable', error);
        return throwError(() => ({
          ...error,
          error: { message: 'Cannot reach the server. Please check your connection or try again.' }
        }));
      }

      switch (error.status) {

        // ── 401 Unauthorised — session expired / invalid token ─────────────
        case 401:
          console.warn('[DSMS] 401 — session expired, redirecting to login');
          auth.logout();
          router.navigate(['/login']);
          return throwError(() => ({
            ...error,
            error: { message: 'Your session has expired. Please log in again.' }
          }));

        // ── 403 Forbidden — logged in but no permission ───────────────────
        case 403:
          return throwError(() => ({
            ...error,
            error: { message: error.error?.message || 'You do not have permission to perform this action.' }
          }));

        // ── 404 Not Found ─────────────────────────────────────────────────
        case 404:
          return throwError(() => ({
            ...error,
            error: { message: error.error?.message || 'The requested resource was not found.' }
          }));

        // ── 400 Bad Request — use server message if present ───────────────
        case 400:
          return throwError(() => ({
            ...error,
            error: { message: error.error?.message || 'Invalid request. Please check your input and try again.' }
          }));

        // ── 500 Internal Server Error ─────────────────────────────────────
        case 500:
          console.error('[DSMS] 500 Server Error', error.error);
          return throwError(() => ({
            ...error,
            error: {
              message: error.error?.message || 'A server error occurred. Please try again or contact support.',
              detail:  error.error?.detail  || null
            }
          }));

        // ── Everything else ───────────────────────────────────────────────
        default:
          console.error(`[DSMS] HTTP ${error.status}`, error);
          return throwError(() => ({
            ...error,
            error: { message: error.error?.message || `Unexpected error (${error.status}). Please try again.` }
          }));
      }
    })
  );
};
