import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  form: FormGroup;
  loading = false;
  showPassword = false;

   constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastr: ToastrService,
  ) {
    this.form = this.fb.group({
      userName: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

   get f() { return this.form.controls; }

   togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;

    this.authService.login(this.form.value).subscribe({
      next: res => {
        this.loading = false;
        if (res.isSuccess) {
          this.toastr.success('Welcome back!');
          // Navigation handled inside AuthService via Router
        } else {
          this.toastr.error(res.message || 'Login failed.');
        }
      },
      error: () => {
        this.loading = false;
      },
    });
  }
}
