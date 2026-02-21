import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Toast } from "primeng/toast";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: '<router-outlet /> <p-toast></p-toast>',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  private readonly translateService = inject(TranslateService);

  constructor() {
    this.translateService.addLangs(['en', 'ar']);
    this.translateService.use('en');
  }
}
