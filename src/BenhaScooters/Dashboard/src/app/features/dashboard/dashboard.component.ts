import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { ProgressBarModule } from 'primeng/progressbar';
import { LoadingService as LoadingService } from '../../core/services/loading';

interface MenuItem {
  label: string;
  icon: string;
  routerLink: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [RouterOutlet, ButtonModule, AvatarModule, TooltipModule, RouterLink, RouterLinkActive, ProgressBarModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly loadingService = inject(LoadingService);

  isSidebarCollapsed = signal(false);
  menuItems: MenuItem[] = [
    {
      label: 'Drivers',
      icon: 'pi pi-car',
      routerLink: '/dashboard/drivers'
    },
    // {
    //   label: 'Parents',
    //   icon: 'pi pi-users',
    //   routerLink: '/dashboard/parents-list'
    // },
    // {
    //   label: 'Videos',
    //   icon: 'pi pi-video',
    //   routerLink: '/dashboard/videos'
    // },
    // {
    //   label: 'Tags',
    //   icon: 'pi pi-tag',
    //   routerLink: '/dashboard/tags'
    // },
    // {
    //   label: 'News',
    //   icon: 'pi pi-receipt',
    //   routerLink: '/dashboard/news'
    // },
    // {
    //   label: 'Contact Us',
    //   icon: 'pi pi-envelope',
    //   routerLink: '/dashboard/contact-us'
    // },
    // {
    //   label: 'Policies',
    //   icon: 'pi pi-file',
    //   routerLink: '/dashboard/policies'
    // }
  ];

  get currentUser() {
    return this.authService.currentUser();
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed.update(collapsed => !collapsed);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
