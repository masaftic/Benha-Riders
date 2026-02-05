# Benha Scooters - Admin Dashboard Setup

## Angular Admin Dashboard

The admin dashboard has been successfully created with Angular 20 and integrated with the ASP.NET Core backend.

### ✅ What Was Implemented

1. **Angular Components**:
   - Login page with JWT authentication
   - Dashboard with driver statistics
   - Drivers list with filtering and pagination
   - Driver details page with approve/reject/suspend actions

2. **Services**:
   - Auth service with JWT token management
   - API service for all HTTP calls to the backend

3. **Backend Integration**:
   - Program.cs configured to serve Angular SPA
   - Static file serving from wwwroot
   - SPA fallback routing
   - Auto-build on publish

### 🚀 How to Run

#### Quick Start (Recommended)

Simply run this command from the project root:

```bash
./run-with-dashboard.sh
```

This script will:
1. Install npm dependencies (if needed)
2. Build the Angular app
3. Start the .NET API with the dashboard

Then navigate to: **http://localhost:5000**

#### Manual Build & Run

1. Build the Angular app:
```bash
cd src/BenhaScooters/DashboardApp
npm install
npm run build
```

2. Run the .NET API:
```bash
cd ../
dotnet run
```

3. Open: **http://localhost:5000**

#### Development Mode (Hot Reload)

For Angular development with hot reload:

Terminal 1 - Start the API:
```bash
cd src/BenhaScooters
dotnet run
```

Terminal 2 - Start Angular dev server:
```bash
cd src/BenhaScooters/DashboardApp
npm start
```

Open: **http://localhost:4200** (proxies API calls to port 5000)

### 🔑 Admin Login Credentials

- **Email**: `admin@benhascooters.com`
- **Password**: `Admin@123`

### 📋 Features

1. **Dashboard**:
   - View counts of Pending, Approved, Rejected, and Suspended drivers
   - Quick navigation to filtered driver lists

2. **Driver List**:
   - Filter by status (Pending, Approved, Rejected, Suspended)
   - Search by name or phone number
   - Pagination support
   - Click to view details

3. **Driver Details**:
   - View complete driver profile
   - View personal information
   - View vehicle information
   - View uploaded documents
   - **Actions**:
     - Approve pending applications
     - Reject applications with reason
     - Suspend approved drivers
     - Unsuspend suspended drivers

### 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         Angular Frontend                │
│    (Port 4200 dev / 5000 prod)         │
│                                         │
│  - Login Component                      │
│  - Dashboard Component                  │
│  - Drivers List Component               │
│  - Driver Details Component             │
│                                         │
│  Services:                              │
│  - AuthService (JWT tokens)             │
│  - ApiService (HTTP client)             │
└─────────────┬───────────────────────────┘
              │
              │ HTTP/REST API
              │
┌─────────────▼───────────────────────────┐
│      ASP.NET Core Backend               │
│           (Port 5000)                   │
│                                         │
│  - JWT Authentication                   │
│  - Admin Endpoints                      │
│  - Driver Management                    │
│  - Static File Serving (wwwroot)        │
│  - SPA Fallback Routing                 │
└─────────────────────────────────────────┘
```

### 📂 Output Location

The Angular build outputs to:
```
src/BenhaScooters/wwwroot/
├── index.html
├── main-*.js
├── polyfills-*.js
└── styles-*.css
```

These files are automatically served by the .NET backend.

### 🔧 Technologies

**Frontend**:
- Angular 20.3
- Standalone Components
- Signals for state management
- TypeScript (strict mode)
- SCSS styling

**Backend**:
- ASP.NET Core 9.0
- Static file middleware
- SPA fallback routing

### 🎯 Next Steps

The dashboard is ready to use! You can:

1. Start the application using `./run-with-dashboard.sh`
2. Login with the admin credentials
3. Manage driver applications
4. Customize the UI/styling as needed

For detailed Angular development instructions, see:
`src/BenhaScooters/DashboardApp/README.md`
