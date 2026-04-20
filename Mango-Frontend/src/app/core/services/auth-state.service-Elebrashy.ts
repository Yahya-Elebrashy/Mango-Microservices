import { Injectable } from '@angular/core';
import { TokenService } from './token.service';
import { BehaviorSubject } from 'rxjs';
import { UserDto } from '../../models';
import { jwtDecode } from 'jwt-decode';
interface JwtPayload {
  sub: string;
  email: string;
  name: string;
  role: string;
  exp: number;
}
@Injectable({
  providedIn: 'root',
})
export class AuthState {
  private _user$ = new BehaviorSubject<UserDto | null>(null);
  private _role$ = new BehaviorSubject<string | null>(null);

  user$ = this._user$.asObservable();
  role$ = this._role$.asObservable();
  constructor(private tokenService: TokenService) {
    // Rehydrate from localStorage on app start
    this.loadFromToken();
  }
  loadFromToken(): void {
    const token = this.tokenService.getToken();
    if (!token) return;

    try {
      const decoded = jwtDecode<JwtPayload>(token);

      // Check expiry
      if (decoded.exp * 1000 < Date.now()) {
        this.clear();
        return;
      }

      this._user$.next({
        id: decoded.sub,
        email: decoded.email,
        name: decoded.name,
        phoneNumber: '',
      });
      this._role$.next(decoded.role);

    } catch {
      this.clear();
    }
  }
  setFromToken(token: string): void {
    this.tokenService.setToken(token);
    this.loadFromToken();
  }
  clear(): void {
    this.tokenService.clearToken();
    this._user$.next(null);
    this._role$.next(null);
  }

  get currentUser(): UserDto | null {
    return this._user$.getValue();
  }

  get currentRole(): string | null {
    return this._role$.getValue();
  }

  get isLoggedIn(): boolean {
    return !!this._user$.getValue();
  }

  get isAdmin(): boolean {
    return this._role$.getValue() === 'ADMIN';
  }
}
