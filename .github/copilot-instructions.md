# Benha Scooters (.NET 10 Backend)

## Architectural Guidelines

- **Clean Architecture**: Preserve the strict separation between `Domain`, `Application`, `Infrastructure`, and `Presentation`.
- **Domain Invariants**: Business logic belongs inside domain entities, aggregate roots, and value objects (`Distance`, `Duration`, `Coordinate`, `LicensePlate`, `NationalId`). Entities protect their own invariants.
- **CQRS & MediatR**: Mutations belong in MediatR Commands with FluentValidation validators. Read models belong in Queries.
- **Result Pattern**: Use `ErrorOr<T>` for anticipated business outcomes and domain validation failures. Avoid using exceptions for normal control flow.
- **Persistence & Spatial**: Leverage EF Core 10 with Npgsql and NetTopologySuite for PostGIS spatial queries (SRID 4326 / WGS84).
- **Asynchronous & Reactive**: Use async/await throughout. Dispatch real-time updates via SignalR hubs and schedule asynchronous work via Hangfire.
