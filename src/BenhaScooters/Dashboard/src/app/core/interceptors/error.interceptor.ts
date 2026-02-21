import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const messageService = inject(MessageService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle HTTP errors
      if (error.error && typeof error.error === 'object') {
        const errorResponse = error.error;

        // Check if it's a validation error with detail message
        if (errorResponse.detail) {
          messageService.add({
            severity: 'error',
            summary: errorResponse.title || 'Validation Error',
            detail: errorResponse.detail,
            life: 5000
          });
        }
        // Check if it's an error with a message property
        else if (errorResponse.message) {
          messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: errorResponse.message,
            life: 5000
          });
        }
        // Generic error with status code
        else {
          messageService.add({
            severity: 'error',
            summary: `Error ${error.status}`,
            detail: error.message || 'An unexpected error occurred',
            life: 5000
          });
        }
      }
      // Handle network errors or unknown errors
      else if (error.status === 0) {
        messageService.add({
          severity: 'error',
          summary: 'Network Error',
          detail: 'Unable to connect to the server. Please check your internet connection.',
          life: 5000
        });
      }
      // Generic error fallback
      else {
        messageService.add({
          severity: 'error',
          summary: `Error ${error.status}`,
          detail: error.message || 'An unexpected error occurred',
          life: 5000
        });
      }

      // Re-throw the error so components can still handle it if needed
      return throwError(() => error);
    })
  );
};
