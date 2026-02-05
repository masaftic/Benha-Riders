import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DriversListComponent } from './pages/drivers/drivers-list.component';
import { DriverDetailsComponent } from './pages/drivers/driver-details.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'drivers', component: DriversListComponent, canActivate: [authGuard] },
  { path: 'drivers/:id', component: DriverDetailsComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/dashboard' }
];
