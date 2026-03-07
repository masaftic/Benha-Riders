import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'drivers',
        pathMatch: 'full'
      },
      {
        path: 'drivers',
        loadComponent: () =>
          import('./features/dashboard/pages/drivers-list/drivers-list.component').then((m) => m.DriversListComponent)
      },
      {
        path: 'drivers/:id',
        loadComponent: () =>
          import('./features/dashboard/pages/driver-details/driver-details.component').then((m) => m.DriverDetailsComponent)
      },
      {
        path: 'contact-us',
        loadComponent: () =>
          import('./features/dashboard/pages/contact-us/contact-us').then((m) => m.ContactUs)
      },
      {
        path: 'policies',
        loadComponent: () =>
          import('./features/dashboard/pages/policies/policies').then((m) => m.PoliciesComponent)
      },
      {
        path: 'policies/:type',
        loadComponent: () =>
          import('./features/dashboard/pages/policies/policy-editor/policy-editor').then((m) => m.PolicyEditor)
      }
    ]
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
