import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CardModule } from 'primeng/card';
import { ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    CardModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  errorMessage = signal<string | null>(null);

  loginForm = this.fb.group({
    phone: ['', [Validators.required, Validators.pattern(/^\+?\d{10,15}$/)]],
    password: ['', [Validators.required, Validators.minLength(3)]]
  });

  onSubmit(): void {
    if (this.loginForm.valid) {
      const { phone, password } = this.loginForm.value;

      this.authService.login(phone!, password!).subscribe({
        next: (success) => {
          if (!success) {
            this.errorMessage.set('Invalid phone or password');
            return;
          }

          const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
          this.router.navigate([returnUrl]);
        },
        error: (error) => {
          this.errorMessage.set('Invalid phone or password');
        }
      });
    } else {
      this.errorMessage.set('Please fill in all required fields');
    }
  }
}
