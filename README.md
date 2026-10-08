# Benha Scooters (BNGO) 🛵

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%20%2B%20PostGIS-336791?logo=postgresql)](https://postgis.net/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20DDD-blue)](#architecture--design-patterns)
[![Tests](https://img.shields.io/badge/Tests-xUnit%20%2B%20Testcontainers-brightgreen)](#testing)

A modular, domain-driven ride-hailing backend for on-demand scooters and light vehicles, designed for high-density urban transit in Benha, Egypt.

Built with **.NET 10**, **Clean Architecture**, and **Domain-Driven Design (DDD)**, featuring geospatial matching with **PostGIS**, real-time dispatch with **SignalR**, distributed background processing with **Hangfire**, and financial ledger tracking for cash-based platform commissions.

---

## 🏛 Architecture & Design Patterns

The solution strictly adheres to **Clean Architecture** and **DDD** principles, ensuring that the domain model is decoupled from framework and infrastructure concerns.

```
Benha-Scooters/
├── src/
│   ├── BenhaScooters.Contracts/    # DTOs, API request/response contracts, client Enums
│   ├── BenhaScooters.Shared/       # Cross-cutting primitives (Security helpers, Localization, Regex)
│   └── BenhaScooters/              # Main application project:
│       ├── Domain/                 # Core business logic: Entities, Value Objects, Domain Events
│       ├── Application/            # CQRS commands/queries (MediatR), FluentValidation rules, abstractions
│       ├── Infrastructure/         # EF Core, PostGIS spatial services, Hangfire, S3, SignalR, Auth
│       └── Presentation/           # REST Controllers, API Versioning middleware, Swagger filters
├── tests/
│   ├── BenhaScooters.UnitTests/        # Fast, isolated domain unit tests
│   └── BenhaScooters.IntegrationTests/ # E2E tests powered by Testcontainers (PostGIS) & Respawn
└── docs/                           # Domain event catalogs, lifecycle specifications, architecture guides
```

### Key Architectural Highlights
- **Rich Domain Entities & Value Objects**: Strict domain invariants encapsulated in types like `Distance`, `Duration`, `Coordinate`, `LicensePlate`, `NationalId`, and `PhoneNumber`.
- **CQRS with MediatR**: Separation of commands (state mutations) and queries (read models), validated automatically through MediatR pipeline behaviors.
- **Domain Event Interceptor**: Aggregates raise domain events dispatched automatically upon `DbContext.SaveChangesAsync()` via EF Core interceptors.
- **Result Pattern (`ErrorOr`)**: Clean, functional error handling avoiding control-flow exceptions.

---

## ⚡ Core Domain Capabilities

### 1. Multi-Round Geospatial Matching
- **Geographic Expansion**: Matches trip requests across progressive search radii (e.g. 500m → 1500m → 2500m) calculated via PostGIS spatial queries.
- **Dynamic Driver Scoring**: Ranks candidate drivers based on proximity and historical rating scores.
- **Scheduled Expiration**: Automatic round advancement and offer timeouts scheduled deterministically via Hangfire background workers.

### 2. Trip Lifecycle State Machine
- Strict state management: `Draft` → `Confirmed` → `DriverAssigned` → `DriverArrived` → `InProgress` → `Completed` / `Cancelled`.
- Guardrails preventing premature trip starts (driver must arrive first) or invalid cancellations once in transit.

### 3. Cash Commission & Wallet Accounting
- Built specifically for cash-dominant transit economies:
  - Riders pay drivers directly in cash upon ride completion.
  - Platform automatically deducts service commission from the driver's digital wallet.
  - Enforces debt limits: drivers exceeding the negative balance threshold are locked from receiving new ride offers until topping up.
  - Supports receipt-based offline cash top-ups reviewed and credited by administrators.

### 4. PostGIS Geofencing & Service Areas
- Enforces polygon-based service boundaries using NetTopologySuite.
- Automatically validates that pickup and destination coordinates fall within licensed operating zones.

### 5. Multi-Layer OTP Security & Rate Limiting
- Progressive cooldowns (60s, 120s, 300s...) to prevent SMS flooding.
- Fraud and brute-force lockout safeguards tracking IP addresses, devices, and phone numbers.

---

## 🛠 Tech Stack

| Component | Technology |
| :--- | :--- |
| **Framework** | .NET 10.0 (C# 13) |
| **Persistence** | PostgreSQL 16 + PostGIS via EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`) |
| **Mediator / CQRS** | MediatR 12 |
| **Validation** | FluentValidation 12 |
| **Real-Time** | ASP.NET Core SignalR (`/hubs/driver`, `/hubs/rider`) |
| **Background Processing** | Hangfire with PostgreSQL storage |
| **Object Storage** | Amazon S3 / MinIO compatible client |
| **Testing** | xUnit, FluentAssertions, Testcontainers (PostGIS), Respawn |
| **Logging** | Serilog (Structured Console + Rolling File Sink) |
| **API Documentation** | OpenAPI / Swagger UI |

---

## 🚀 Quickstart & Local Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) & Docker Compose

### 1. Start Infrastructure (Postgres PostGIS + MinIO)
Run the bundled Docker Compose file to start a local PostGIS database and MinIO storage container:

```bash
docker compose up -d postgres minio
```

### 2. Configure Environment
A default local development configuration is provided in `src/BenhaScooters/appsettings.Development.json`.

Optionally set your secrets using .NET User Secrets:
```bash
cd src/BenhaScooters
dotnet user-secrets set "Jwt:SigningKey" "your_local_development_jwt_secret_key_at_least_32_chars!"
dotnet user-secrets set "GoogleMaps:ApiKey" "your_optional_google_maps_api_key"
```

### 3. Run the Application
```bash
dotnet run --project src/BenhaScooters/BenhaScooters.csproj
```

The database migrations and initial seed data (service area polygons, demo admin, sample verified drivers) will apply automatically on startup in development mode.

- **Swagger UI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Hangfire Dashboard**: [http://localhost:5000/hangfire](http://localhost:5000/hangfire)
- **Driver SignalR Hub**: `ws://localhost:5000/hubs/driver`
- **Rider SignalR Hub**: `ws://localhost:5000/hubs/rider`

> **Note on OTP Testing**: In local development, `OtpSecurity:UseFixedOtp` is enabled by default with fixed code `123456` for frictionless API testing without external SMS gateway dependencies.

---

## 🧪 Testing

The solution includes an automated test suite featuring **Testcontainers** to run tests against a real PostGIS PostgreSQL engine:

```bash
# Run all tests
dotnet test
```

- **Unit Tests**: `tests/BenhaScooters.UnitTests` validates domain rules, aggregates, and calculations in isolation.
- **Integration Tests**: `tests/BenhaScooters.IntegrationTests` validates full API pipelines, database queries, and middleware using real disposable PostGIS Docker containers reset via Respawn between tests.

---

## 📖 Additional Documentation

- [Domain Event Catalog](docs/DomainEventCatalog.md) — Comprehensive reference of domain events, publishers, and handlers.
- [Matching Workflow Guide](docs/MatchingMessageWorkflowGuide.md) — Multi-round matching state progression details.
- [Launch & Scale Plan](docs/LAUNCH_PLAN.md) — Roadmap for production hardening and infrastructure scaling.
