import { Injectable, signal } from '@angular/core';

/**
 * Holds the bearer token for the auth interceptor to attach. There's no login UI in this app
 * (a deliberate scope decision from Day 13/14) — this exists so the interceptor itself is real
 * and testable, with `setToken()` as the seam a future login flow would call.
 */
@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly _token = signal<string | null>(null);
  readonly token = this._token.asReadonly();

  setToken(token: string | null): void {
    this._token.set(token);
  }
}
