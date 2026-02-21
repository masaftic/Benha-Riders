# Dashboard Structure Guide

## Current Implementation

### Authentication Flow
- **Login Page** (`/login`): Email and password authentication
- **Auth Service**: Signal-based authentication state management
- **Auth Guard**: Protects dashboard routes from unauthorized access

### Dashboard Layout
- **Custom Collapsible Sidebar**: Can toggle between expanded (w-64) and collapsed (w-20) states
- **Custom Navigation Menu**: Simple top-level menu items with icons, labels, and routing
- **Main Content Area**: Displays routed pages with top navigation bar
- **Tooltips**: Menu items show tooltips when sidebar is collapsed

### File Structure
```
src/app/
├── app.ts                    # Root component with router-outlet
├── app.routes.ts             # Main routing configuration
├── core/
│   ├── guards/
│   │   └── auth.guard.ts     # Route protection
│   └── services/
│       └── auth.service.ts   # Authentication logic
└── features/
    ├── auth/
    │   └── login/            # Login page
    └── dashboard/
        ├── dashboard.component.*  # Main layout with sidebar
        └── pages/
            └── home/         # Dashboard home page
```

## How to Add New Pages

### 1. Create a New Page Component

Use Angular CLI or create manually:
```bash
# Using Angular CLI
ng generate component features/dashboard/pages/your-page-name

# File structure will be:
# features/dashboard/pages/your-page-name/
#   ├── your-page-name.component.ts
#   ├── your-page-name.component.html
#   └── your-page-name.component.scss
```

**Component Template:**
```typescript
import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-your-page-name',
  imports: [CardModule],
  templateUrl: './your-page-name.component.html',
  styleUrl: './your-page-name.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class YourPageNameComponent {}
```

### 2. Add Route to Dashboard Children

Update `app.routes.ts`:
```typescript
{
  path: 'dashboard',
  loadComponent: () =>
    import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  canActivate: [authGuard],
  children: [
    // ... existing routes
    {
      path: 'your-page-name',
      loadComponent: () =>
        import('./features/dashboard/pages/your-page-name/your-page-name.component')
          .then((m) => m.YourPageNameComponent)
    }
  ]
}
```

### 3. Add Menu Item to Sidebar

Update `dashboard.component.ts`:
```typescript
menuItems: MenuItem[] = [
  // ... existing items
  {
    label: 'Your Page',
    icon: 'pi pi-your-icon',  // See PrimeIcons for available icons
    routerLink: '/dashboard/your-page-name'
  }
];
```

The menu interface is:
```typescript
interface MenuItem {
  label: string;      // Display text
  icon: string;       // PrimeIcons class (e.g., 'pi pi-home')
  routerLink: string; // Route path
}
```

## Best Practices for Future Development

### Component Structure
- Keep components small and focused
- Use signals for state management
- Set `changeDetection: ChangeDetectionStrategy.OnPush`
- Use reactive forms for user input

### Shared Components
Create shared components in:
```
src/app/shared/
├── components/      # Reusable UI components
├── directives/      # Custom directives
└── pipes/          # Custom pipes
```

### Feature Modules
For larger features, organize as:
```
src/app/features/your-feature/
├── components/     # Feature-specific components
├── services/       # Feature-specific services
├── models/         # TypeScript interfaces/types
└── your-feature.component.ts  # Main feature component
```

### Services
- Keep services in `core/services/` for global services
- Use `providedIn: 'root'` for singleton services
- Use `inject()` function instead of constructor injection

### Routing
- Use lazy loading for all feature routes
- Group related routes under common parent paths
- Use guards for route protection

### Styling
- Use Tailwind CSS utility classes
- Use PrimeNG components for UI elements
- Keep component-specific styles in `.scss` files
- Use theme variables from PrimeNG preset

## Available PrimeNG Components
- **Forms**: InputText, Password, Select, Calendar, etc.
- **Buttons**: Button, SplitButton, ToggleButton
- **Data**: Table, DataView, Tree
- **Panels**: Card, Panel, Accordion, TabView
- **Overlays**: Dialog, Sidebar, Toast, ConfirmDialog
- **Menus**: Menu, Menubar, PanelMenu, MegaMenu
- **Charts**: Chart component with Chart.js integration
- **Messages**: Message, Toast
- **File**: FileUpload

## Icons
Use PrimeIcons: https://primeng.org/icons
- `pi pi-home`
- `pi pi-user`
- `pi pi-cog`
- `pi pi-chart-line`
- etc.

## Authentication
- Login credentials are stored in localStorage (replace with real API)
- Auth state is managed with signals
- Protected routes use `authGuard`
- Logout clears localStorage and redirects to login

## Internationalization (i18n)
- Already configured with `@ngx-translate/core`
- Translation files: `public/assets/i18n/en.json`, `ar.json`
- Current languages: English (en), Arabic (ar)
- Add translations to JSON files and use `translate` directive or pipe
