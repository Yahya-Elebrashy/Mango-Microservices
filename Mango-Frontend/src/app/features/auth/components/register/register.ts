import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { Router, RouterLink } from '@angular/router';
import { SD } from '../../../../constants/app.constants';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  form: FormGroup;
  loading = false;
  showPassword = false;
  roles = [SD.ROLE_CUSTOMER, SD.ROLE_ADMIN];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastr: ToastrService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      name:        ['', [Validators.required, Validators.minLength(2)]],
      email:       ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?[0-9]{7,15}$/)]],
      password:    ['', [Validators.required, Validators.minLength(6)]],
      role:        [SD.ROLE_CUSTOMER],
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

    this.authService.register(this.form.value).subscribe({
      next: res => {
        if (res.isSuccess) {
          // Assign role after successful registration
          this.authService.assignRole(this.form.value).subscribe({
            next: roleRes => {
              this.loading = false;
              if (roleRes.isSuccess) {
                this.toastr.success('Registration successful! Please log in.');
                this.router.navigate(['/auth/login']);
              } else {
                this.toastr.warning('Registered but role assignment failed.');
                this.router.navigate(['/auth/login']);
              }
            },
            error: () => { this.loading = false; },
          });
        } else {
          this.loading = false;
          this.toastr.error(res.message || 'Registration failed.');
        }
      },
      error: () => { this.loading = false; },
    });
  }
}
