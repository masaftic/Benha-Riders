# Domain Event Catalog

This document summarizes the main domain events in the system, their publishers (aggregates), and primary handlers. It also includes some guidelines for what belongs in an event handler.

---

## Matching & Trip Request Events

### TripRequestedEvent
- **Type**: `TripRequestedEvent`  
- **Namespace**: `BenhaScooters.Domain.TripRequests.Events`  
- **Raised by**: `TripRequest.CreateTripRequestedEvent()` in `TripRequest`  
- **When**: A new trip request is created.  
- **Handlers**: *(none yet – currently informational only)*  
- **Typical responsibilities**: Analytics, notifications, auditing.

### TripRequestConfirmedEvent
- **Type**: `TripRequestConfirmedEvent`  
- **Raised by**: `TripRequest.Confirm()` in `TripRequest`  
- **When**: Rider confirms a trip request.  
- **Handlers**:
  - `TripRequestConfirmedEventHandler`  
    - Creates a `MatchingSession` via `CreateMatchingSessionCommand`.  
    - Starts the first matching round via `IDriverMatchingService.ProcessMatchingAsync`.

### MatchingSessionCancelledEvent
- **Type**: `MatchingSessionCancelledEvent`  
- **Raised by**: `MatchingSession.Cancel(...)` and within `MatchingSession.TryTransitionToNextRound()` when all rounds are exhausted.  
- **When**: Matching session is cancelled (no drivers or all rounds completed without a match).  
- **Handlers**:
  - `MatchingSessionCancelledEventHandler`  
    - Loads the associated `TripRequest`.  
    - Calls `TripRequest.Cancel(reason)` and saves changes.

### TripMatchAcceptedEvent
- **Type**: `TripMatchAcceptedEvent`  
- **Namespace**: `BenhaScooters.Domain.Matching.Events`  
- **Raised by**: `MatchingSession.AcceptMatch(DriverId)`  
- **When**: A driver accepts a pending match attempt. The session is completed at this point.  
- **Handlers**:
  - `TripMatchAcceptedEventHandler`  
    - Currently logs the accepted match.  
    - Reserved for future cross-cutting concerns (analytics, notifications).

### TripMatchRejectedEvent
- **Type**: `TripMatchRejectedEvent`  
- **Raised by**: `MatchingSession.RejectMatch(DriverId, reason)`  
- **When**: A driver explicitly rejects a match offer.  
- **Handlers**:
  - `TripMatchRejectedEventHandler`  
    - Logs the rejection.  
    - Delegates to `IMatchingOrchestrator.HandlePostOutcomeAsync(tripRequestId)` for round advancement / cancellation.

### TripMatchExpiredEvent
- **Type**: `TripMatchExpiredEvent`  
- **Raised by**: `MatchingSession.ExpireMatch(DriverId)`  
- **When**: A match attempt expires (no response within timeout).  
- **Handlers**:
  - Implicitly handled through `DriverMatchingService.HandleMatchAttemptTimeoutAsync`, which:
    - Calls `ExpireMatch(driverId)` on the session.  
    - Saves changes.  
    - Delegates to `IMatchingOrchestrator.HandlePostOutcomeAsync(tripRequestId)`.

### DriverMatchOfferCreatedEvent
- **Type**: `DriverMatchOfferCreatedEvent`  
- **Raised by**: `MatchingSession.CreateDriverMatchAttempt(...)`  
- **When**: A match attempt (offer) is created for a driver in a given round.  
- **Handlers**: *(none yet – offers are currently surfaced via SignalR from `DriverMatchingService` rather than this event)*.

---

## Trip Lifecycle Events

These are raised by the `Trip` aggregate as the ride progresses.

### TripCreatedEvent
- **Type**: `TripCreatedEvent`  
- **Raised by**: `trip.CreateTripCreatedEvent()` (published from `AcceptMatchCommandHandler` after trip creation).  
- **When**: Immediately after a `Trip` is created and persisted as part of accepting a match.  
- **Handlers**:
  - `TripCreatedEventHandler`  
    - Currently logs the creation; future responsibilities may include rider/driver notifications.

### DriverArrivedEvent
- **Type**: `DriverArrivedEvent`  
- **Raised by**: `Trip.DriverArrived()`  
- **When**: Driver marks that they have arrived at the pickup location.  
- **Handlers**:
  - `DriverArrivedEventHandler`  
    - Logs driver arrival.  
    - TODO: notify rider (push or in-app notification).

### TripStartedEvent
- **Type**: `TripStartedEvent`  
- **Raised by**: `Trip.StartTrip()`  
- **When**: Trip moves to `InProgress`.  
- **Handlers**:
  - `TripStartedEventHandler`  
    - Logs the start of the trip.  
    - Suitable place for analytics or billing meter start.

### TripCompletedEvent
- **Type**: `TripCompletedEvent`  
- **Raised by**: `Trip.CompleteTrip()`  
- **When**: Trip is successfully completed.  
- **Handlers**: *(to be extended – e.g., final fare calculation, payment initiation, notifications).*  

---

## Driver Domain Events (overview)

There are several driver-related events used mainly for onboarding and lifecycle:

- `DriverRegisteredEvent`  
- `DriverActivatedEvent`  
- `DriverDeactivatedEvent`  
- `DriverOnboardingCompletedEvent`  
- `DriverOnboardingRejectedEvent`  
- `DriverOnboardingStepCompletedEvent`  

Handlers live under `Application/Features/Drivers/EventHandlers` and are mostly responsible for initializing driver status, location, and rating. These are orthogonal to the matching/trip flow.

---

## Rules for What Belongs in an Event Handler

Use these heuristics to decide what logic should live in a handler and when to split it.

1. **Single responsibility per handler**  
   - A handler should express **one clear reaction** to an event (e.g., "create trip", "send notification", "update read model").  
   - If you find a handler doing multiple unrelated things (DB writes + email + analytics), split them into separate handlers for the same event.

2. **Aggregates publish, handlers orchestrate**  
   - Aggregates (`MatchingSession`, `TripRequest`, `Trip`) should **only** raise events about their own state changes.  
   - Handlers are allowed to:
     - Load other aggregates from the database.  
     - Coordinate multiple aggregates in a transaction (e.g., create `Trip`, update `DriverStatus`, mark `TripRequest` as matched).  
   - If you catch yourself calling repositories or services from inside an aggregate, that logic likely belongs in a handler instead.

3. **Keep handlers cohesive and readable**  
   - If a handler grows beyond a small, readable flow (for example, many nested `if`s and multiple branches), consider:
     - Extracting an application service (like `MatchingOrchestrator`) that the handler calls.  
     - Or splitting into multiple handlers for different concerns (e.g., one for persistence, one for notifications).

4. **Side effects vs. notifications**  
   - Distinguish between:
     - **Orchestrating handlers**: change state of multiple aggregates and may open transactions (e.g., `TripMatchAcceptedEventHandler`).  
     - **Notification handlers**: only log or push messages (e.g., `DriverArrivedEventHandler`).  
   - If a notification concern starts to require database writes or complex branching, promote it to its own orchestrating handler or service.

5. **Stop when the handler starts "knowing too much"**  
   - If a handler must know about many unrelated domains (e.g., matching + billing + notifications), that’s a signal to stop and move some logic elsewhere.  
   - Ask: *"Could this piece be triggered by its own, more specific event?"* If yes, consider raising another domain event and handling that separately.

6. **Error handling and logging**  
   - Handlers should:
     - Log failures with enough IDs (TripRequestId, MatchingSessionId, DriverId, TripId).  
     - Return or stop gracefully on failure; do not swallow exceptions silently.  
   - If error handling dominates the core logic, extract the core steps into an application service and keep the handler as a thin wrapper.

7. **When to introduce a new event vs. a new handler**  
   - New **event**: when you need to express a new, meaningful state change in the domain language (e.g., `TripCancelledEvent`).  
   - New **handler**: when you want an additional reaction to an existing event (e.g., sending analytics when a trip is created) without changing the aggregate.

By following these rules:
- Aggregates stay focused and small.  
- Event handlers read like clear stories for each important state change.  
- Adding new behaviors becomes a matter of adding new handlers, not touching core domain logic.
