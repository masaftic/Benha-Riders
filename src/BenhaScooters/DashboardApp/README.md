# Admin Dashboard - Angular

This is the admin dashboard for Benha Scooters, built with Angular 20 and integrated with the ASP.NET Core backend.

## Features

- **Authentication**: Login with JWT tokens
- **Dashboard**: View driver statistics (Pending, Approved, Rejected, Suspended)
- **Driver Management**: 
  - List drivers with filtering and pagination
  - View detailed driver information
  - Approve pending applications
  - Reject applications with reason
  - Suspend/unsuspend drivers

## Development Setup

### Prerequisites

- Node.js 18+ and npm
- .NET 9.0 SDK

### Running in Development Mode

#### Option 1: Standalone Angular Development Server

1. Navigate to the DashboardApp directory:
   ```bash
   cd src/BenhaScooters/DashboardApp
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the Angular dev server (with API proxy):
   ```bash
   npm start
   ```

4. In a separate terminal, start the .NET API:
   ```bash
   cd src/BenhaScooters
   dotnet run
   ```

The Angular app will run on http://localhost:4200 and proxy API calls to http://localhost:5000.

#### Option 2: Integrated with .NET Backend

1. Build the Angular app:
   ```bash
   cd src/BenhaScooters/DashboardApp
   npm install
   npm run build
   ```

2. Run the .NET application:
   ```bash
   cd src/BenhaScooters
   dotnet run
   ```

The app will be served directly from the .NET backend at http://localhost:5000.

## Production Build

When you publish the .NET application, the Angular app is automatically built and included:

```bash
cd src/BenhaScooters
dotnet publish -c Release
```

The Angular build is configured to output to `wwwroot/` which is served by the .NET backend.

## Default Admin Credentials

- Email: `admin@benhascooters.com`
- Password: `Admin@123`

## Project Structure

```
DashboardApp/
├── src/
│   ├── app/
│   │   ├── guards/
│   │   │   └── auth.guard.ts          # Route authentication guard
│   │   ├── models/
│   │   │   └── driver.models.ts       # TypeScript interfaces
│   │   ├── pages/
│   │   │   ├── login/
│   │   │   │   └── login.component.ts
│   │   │   ├── dashboard/
│   │   │   │   └── dashboard.component.ts
│   │   │   └── drivers/
│   │   │       ├── drivers-list.component.ts
│   │   │       └── driver-details.component.ts
│   │   ├── services/
│   │   │   ├── auth.service.ts        # Authentication service
│   │   │   └── api.service.ts         # HTTP API client
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts
│   ├── styles.scss
│   └── main.ts
├── angular.json
├── package.json
└── proxy.conf.json                     # Dev server proxy config
```

## API Endpoints Used

- `POST /api/auth/login` - Authentication
- `GET /api/admin/drivers` - List drivers (with filters)
- `GET /api/admin/drivers/{id}` - Driver details
- `POST /api/admin/drivers/{id}/approve` - Approve driver
- `POST /api/admin/drivers/{id}/reject` - Reject driver
- `POST /api/admin/drivers/{id}/ban` - Suspend driver
- `POST /api/admin/drivers/{id}/unban` - Unsuspend driver

## Technologies

- **Angular 20.3** - Framework
- **Standalone Components** - Modern Angular architecture
- **Signals** - Reactive state management
- **TypeScript** - Type safety
- **SCSS** - Styling
- **RxJS** - Async operations

---

## Angular CLI Commands

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.11.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
