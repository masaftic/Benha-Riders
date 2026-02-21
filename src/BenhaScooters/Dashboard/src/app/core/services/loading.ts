import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class LoadingService {
  loading = signal(false);

  setLoading(isLoading: boolean) {
    this.loading.set(isLoading);
  }

  isLoading() {
    return this.loading();
  }
}
