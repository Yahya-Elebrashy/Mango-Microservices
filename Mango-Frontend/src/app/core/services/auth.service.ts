import { Injectable } from "@angular/core";
import { environment } from '../../../environments/environment';
import { ApiService } from "./api.service";
import { Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { LoginRequestDto, LoginResponseDto, RegisterRequestDto, ResponseDto } from "../../models";
import { Observable, tap } from "rxjs";
import { AuthState } from "./auth-state.service";

@Injectable({ providedIn: 'root' })
export class AuthService {

    private base = environment.authApiBase;

    constructor(
        private api: ApiService,
        private authState: AuthState,
        private router: Router,
        private toastr: ToastrService,
    ) {}

    login(payload: LoginRequestDto): Observable<ResponseDto<LoginResponseDto>> {
    return this.api.post<LoginResponseDto>(
      `${this.base}/api/auth/login`,
      payload
    ).pipe(
      tap(res => {
        if (res.isSuccess && res.result?.token) {
          this.authState.setFromToken(res.result.token);
          this.router.navigate(['/']);
        }
      })
    );
  }

   register(payload: RegisterRequestDto): Observable<ResponseDto<unknown>> {
    return this.api.post(`${this.base}/api/auth/register`, payload);
  }

  assignRole(payload: RegisterRequestDto): Observable<ResponseDto<unknown>> {
    return this.api.post(`${this.base}/api/auth/assignRole`, payload);
  }

  logout(): void {
    this.authState.clear();
    this.toastr.success('Logged out successfully.');
    this.router.navigate(['/auth/login']);
  }
}