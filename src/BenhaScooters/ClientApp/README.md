# Admin Dashboard - React Frontend

This is the React + TypeScript frontend for the Benha Scooters admin dashboard.

## Features

- ✅ Modern React with Vite for fast development
- ✅ **TypeScript** for type safety
- ✅ Tailwind CSS for styling
- ✅ JWT authentication with token refresh
- ✅ Driver onboarding approval/rejection
- ✅ View driver details and documents
- ✅ Responsive design

## Development

To run the React app in development mode:

```bash
cd ClientApp
npm install
npm run dev
```

The development server will start on http://localhost:5173

## Type Checking

To check TypeScript types without building:

```bash
npm run type-check
```

## Building

The React app is automatically built when you build the .NET project:

```bash
dotnet build
```

Or build manually:

```bash
cd ClientApp
npm run build
```

## Production

When you run the .NET application, it serves the built React app from the `wwwroot` folder:

```bash
dotnet run
```

Then access the admin dashboard at: http://localhost:5000/

## Default Admin Credentials

According to the DataSeeder:
- Phone: `01234567890`
- Password: `password`

## API Endpoints Used

- `POST /api/auth/login` - Admin login
- `POST /api/auth/logout` - Logout
- `POST /api/auth/refresh` - Refresh token
- `GET /api/admin/drivers` - List drivers with filters
- `GET /api/admin/drivers/{id}` - Get driver details
- `GET /api/admin/drivers/{id}/documents` - Get driver documents
- `POST /api/admin/drivers/{id}/approve` - Approve driver
- `POST /api/admin/drivers/{id}/ban` - Ban driver
