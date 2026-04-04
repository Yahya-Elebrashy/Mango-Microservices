import { Injectable } from '@angular/core';
import { SD } from '../../constants/app.constants';

@Injectable({ providedIn: 'root' })
export class TokenService {

  setToken(token: string): void {
    localStorage.setItem(SD.TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(SD.TOKEN_KEY);
  }

  clearToken(): void {
    localStorage.removeItem(SD.TOKEN_KEY);
  }

  hasToken(): boolean {
    return !!this.getToken();
  }
}