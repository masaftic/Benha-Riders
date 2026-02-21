import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';


interface AuthResponse {
  type: string;
  result: {
    accessToken: string;
    refreshToken: string;
  }
}


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly router = Router;
  private apiUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  private isAuthenticated = signal(false);
  currentUser = signal<{ email: string } | null>(null);

  constructor() {
    // Check localStorage for existing session
    const storedAuth = localStorage.getItem('isAuthenticated');
    const storedUser = localStorage.getItem('currentUser');

    if (storedAuth === 'true' && storedUser) {
      this.isAuthenticated.set(true);
      this.currentUser.set(JSON.parse(storedUser));
    }
  }

  verifyUserAuth(): boolean {
    // extract expire time from token in localStorage
    const token = localStorage.getItem('token');
    if (!token) {
      return false;
    }

    const decodedToken: any = jwtDecode(token);
    // if (decodedToken.scope != 'admin') {
    //   this.logout();
    //   return false;
    // }
    const currentTime = Math.floor(Date.now() / 1000);

    if (decodedToken.exp < currentTime) {
      this.logout();
      return false;
    }

    return this.isAuthenticated();
  }

  login(phone: string, password: string): Observable<boolean> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, { phoneNumber: phone, password }).pipe(
      tap((response) => {
        const decodedToken: any = jwtDecode(response.result.accessToken);
        // if (decodedToken.scope != 'admin') {
        //   throw new Error('Unauthorized scope');
        // }

        this.isAuthenticated.set(true);
        this.currentUser.set({ email: decodedToken.email });

        localStorage.setItem('isAuthenticated', 'true');
        localStorage.setItem('token', response.result.accessToken);
        localStorage.setItem('currentUser', JSON.stringify({ email: decodedToken.email }));

        console.log('Decoded Token:', decodedToken);
      }),
      map(() => true),
      catchError((error) => {
        return of(false);
      })
    );
  }

  logout(): void {
    this.isAuthenticated.set(false);
    this.currentUser.set(null);

    // Clear localStorage
    localStorage.removeItem('isAuthenticated');
    localStorage.removeItem('currentUser');
    localStorage.removeItem('token');
  }
}
