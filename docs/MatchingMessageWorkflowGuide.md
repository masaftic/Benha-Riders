# Matching Message Workflow Guide

This note is meant to make one thing easier to hold in your head:

- what a **domain event** is
- what a **workflow message** is
- how the current matching flow could slowly move toward message-driven orchestration

This is not a big-bang redesign. It is a way to make the matching flow easier to read, reason about, and change without breaking the app.

---

## The Short Version

### Domain event

A domain event says:

> "Something important already happened inside this aggregate."

Examples from this codebase:

- `TripRequestConfirmedEvent`
- `MatchAttemptAcceptedEvent`
- `MatchAttemptRejectedEvent`
- `MatchingSessionCancelledEvent`

These events come **out of domain objects** like `TripRequest` and `MatchingSession`.

They describe facts in domain language.

### Workflow message

A workflow message says:

> "Please do the next step in this process."

Examples we could introduce:

- `StartMatchingSession`
- `StartMatchingRound`
- `MatchingRoundTimedOut`
- `EvaluateMatchingProgress`
- `FinalizeMatchingSession`

These messages do **not** describe a business fact that already happened inside one aggregate.

They describe work the application should perform next.

---

## The Main Difference

This is the distinction that usually makes it click:

- **Domain event** = past tense, business fact, raised by aggregate
- **Workflow message** = imperative or process step, sent by application code

Examples:

- `TripRequestConfirmedEvent`
  - Means the rider already confirmed the request.
  - Raised by `TripRequest.Confirm()`.

- `StartMatchingRound`
  - Means the application should now try to send offers for round `N`.
  - Sent by a handler or application service.

Another way to say it:

- Domain events tell us what happened
- Workflow messages tell the system what to do next

---

## Why The Current Flow Feels Hard To Remember

Today the matching process is spread across:

- a domain event: `TripRequestConfirmedEvent`
- a handler that creates a session and enqueues matching
- `DriverMatchingService`
- `MatchingOrchestrator`
- Hangfire jobs that call service methods later

That means the process is there, but it is not represented as a clear sequence of named steps.

Instead, the sequence is hidden in:

- method calls
- scheduled service calls
- "after this, maybe that service calls the other service"

That is exactly where workflow messages help.

---

## The Mental Model To Use

Try to picture matching as a queue of small steps:

1. rider confirmed trip request
2. create matching session
3. start round 1
4. send offers
5. wait for timeout or driver response
6. evaluate outcome
7. either advance to next round or cancel or complete

That list is your workflow.

A message-driven workflow just makes each step explicit in code.

---

## What Stays As Domain Events

These should remain domain events because they are facts from aggregates:

```csharp
public ErrorOr<Success> Confirm()
{
    if (Status != TripRequestStatus.NotConfirmed)
        return AppErrors.TripRequest.AlreadyConfirmed();

    Status = TripRequestStatus.Pending;
    ConfirmedAt = DateTime.UtcNow;

    RaiseDomainEvent(new TripRequestConfirmedEvent(
        Id,
        RiderId,
        PickupLocation,
        DropoffLocation,
        PickupAddress,
        DropoffAddress,
        FinalFare,
        DateTime.UtcNow));

    return Result.Success;
}
```

And inside matching:

```csharp
public ErrorOr<Success> AcceptMatch(UserId driverId)
{
    // mutate aggregate state

    RaiseDomainEvent(new MatchAttemptAcceptedEvent(
        TripRequestId,
        driverId,
        matchAttempt.DistanceToPickup,
        matchAttempt.EstimatedArrivalTime,
        DateTime.UtcNow));

    return Result.Success;
}
```

These are good domain events because they are about state changes that happened inside one aggregate.

---

## What Becomes Workflow Messages

These are good candidates for workflow messages:

```csharp
public record StartMatchingSession(TripRequestId TripRequestId);

public record StartMatchingRound(MatchingSessionId MatchingSessionId);

public record MatchingRoundTimedOut(MatchingSessionId MatchingSessionId, int RoundNumber);

public record EvaluateMatchingProgress(MatchingSessionId MatchingSessionId);
```

Notice the difference:

- they are not saying "the aggregate raised me"
- they are saying "please go do this step"

---

## The Flow We Want

### Step 1: rider confirms trip request

Domain event:

```csharp
TripRequestConfirmedEvent
```

Handler reaction:

```csharp
public class TripRequestConfirmedEventHandler : INotificationHandler<TripRequestConfirmedEvent>
{
    private readonly IMessageScheduler _scheduler;

    public TripRequestConfirmedEventHandler(IMessageScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task Handle(TripRequestConfirmedEvent notification, CancellationToken ct)
    {
        await _scheduler.EnqueueAsync(
            new StartMatchingSession(notification.TripRequestId),
            ct);
    }
}
```

Important idea:

- the domain event handler does not start doing all the matching work
- it just kicks off the workflow

---

## Step 2: create or load the session

```csharp
public class StartMatchingSessionHandler : IRequestHandler<StartMatchingSession>
{
    private readonly AppDbContext _db;
    private readonly IOptions<MatchingSessionOptions> _options;
    private readonly IMessageScheduler _scheduler;

    public StartMatchingSessionHandler(
        AppDbContext db,
        IOptions<MatchingSessionOptions> options,
        IMessageScheduler scheduler)
    {
        _db = db;
        _options = options;
        _scheduler = scheduler;
    }

    public async Task Handle(StartMatchingSession command, CancellationToken ct)
    {
        var tripRequest = await _db.TripRequests
            .FirstOrDefaultAsync(x => x.Id == command.TripRequestId, ct);

        if (tripRequest is null || !tripRequest.CanBeAssigned)
            return;

        var session = await _db.MatchingSessions
            .FirstOrDefaultAsync(x => x.TripRequestId == command.TripRequestId, ct);

        if (session is null)
        {
            var createResult = MatchingSession.Create(
                command.TripRequestId,
                _options.Value.NumberOfRounds,
                _options.Value.OffersPerRound.ToList());

            if (createResult.IsError)
                return;

            session = createResult.Value;
            _db.MatchingSessions.Add(session);
            await _db.SaveChangesAsync(ct);
        }

        await _scheduler.EnqueueAsync(new StartMatchingRound(session.Id), ct);
    }
}
```

This handler has one job:

- ensure a session exists
- enqueue the next step

That is much easier to remember than "the event handler calls a command and then a background service."

---

## Step 3: start one round

```csharp
public class StartMatchingRoundHandler : IRequestHandler<StartMatchingRound>
{
    private readonly AppDbContext _db;
    private readonly IDriverRankingService _driverRanking;
    private readonly IGeoService _geoService;
    private readonly IPublisher _publisher;
    private readonly IMessageScheduler _scheduler;
    private readonly MatchingSessionOptions _options;

    public StartMatchingRoundHandler(
        AppDbContext db,
        IDriverRankingService driverRanking,
        IGeoService geoService,
        IPublisher publisher,
        IMessageScheduler scheduler,
        IOptions<MatchingSessionOptions> options)
    {
        _db = db;
        _driverRanking = driverRanking;
        _geoService = geoService;
        _publisher = publisher;
        _scheduler = scheduler;
        _options = options.Value;
    }

    public async Task Handle(StartMatchingRound message, CancellationToken ct)
    {
        var session = await _db.MatchingSessions
            .Include(x => x.MatchAttempts)
            .Include(x => x.TripRequest)
                .ThenInclude(x => x.RiderProfile)
                    .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == message.MatchingSessionId, ct);

        if (session is null || !session.IsActive)
            return;

        var tripRequest = session.TripRequest;

        var drivers = await _driverRanking.FindTopNDriversAsync(
            tripRequest.PickupLocation,
            session.CurrentOffer,
            session.CurrentRound,
            session.GetRejectedOrPendingDrivers(),
            ct);

        foreach (var driver in drivers)
        {
            var eta = _geoService.EstimateArrivalTime(driver.DistanceToPickup);

            var attemptResult = session.CreateDriverMatchAttempt(
                driver.DriverId,
                driver.DistanceToPickup,
                eta,
                driver.Score);

            if (attemptResult.IsError)
                continue;
        }

        await _db.SaveChangesAsync(ct);

        foreach (var attempt in session.MatchAttempts.Where(x =>
                     x.MatchingRound == session.CurrentRound &&
                     x.Status == MatchAttemptStatus.Pending))
        {
            await _publisher.Publish(new DriverMatchOfferCreatedEvent(
                attempt.Id,
                tripRequest.Id,
                attempt.DriverUserId,
                tripRequest.RiderProfile.PreferredName ?? tripRequest.RiderProfile.User.Name,
                tripRequest.PickupCoordinate,
                tripRequest.DropoffCoordinate,
                tripRequest.PickupAddress,
                tripRequest.DropoffAddress,
                tripRequest.FinalFare.Amount,
                tripRequest.FinalFare.Distance,
                attempt.DistanceToPickup,
                attempt.EstimatedArrivalTime,
                attempt.CreatedAt), ct);
        }

        await _scheduler.ScheduleAsync(
            new MatchingRoundTimedOut(session.Id, session.CurrentRound),
            _options.RoundTimeout,
            ct);
    }
}
```

This replaces a big chunk of today's `DriverMatchingService`.

The nice part is that the name says the intent clearly:

- `StartMatchingRoundHandler`

When you come back later, you do not have to remember hidden control flow between services.

---

## Step 4: timeout is just another message

```csharp
public class MatchingRoundTimedOutHandler : IRequestHandler<MatchingRoundTimedOut>
{
    private readonly IMessageScheduler _scheduler;

    public MatchingRoundTimedOutHandler(IMessageScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task Handle(MatchingRoundTimedOut message, CancellationToken ct)
    {
        await _scheduler.EnqueueAsync(
            new EvaluateMatchingProgress(message.MatchingSessionId),
            ct);
    }
}
```

This looks almost too small, but that is good.

Timeout happened.
Now evaluate.

That is the entire story.

---

## Step 5: one place decides what happens next

```csharp
public class EvaluateMatchingProgressHandler : IRequestHandler<EvaluateMatchingProgress>
{
    private readonly AppDbContext _db;
    private readonly IMessageScheduler _scheduler;

    public EvaluateMatchingProgressHandler(AppDbContext db, IMessageScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public async Task Handle(EvaluateMatchingProgress message, CancellationToken ct)
    {
        var session = await _db.MatchingSessions
            .Include(x => x.MatchAttempts)
            .FirstOrDefaultAsync(x => x.Id == message.MatchingSessionId, ct);

        if (session is null || !session.IsActive)
            return;

        var accepted = session.MatchAttempts.Any(x => x.Status == MatchAttemptStatus.Accepted);
        if (accepted)
            return;

        var stillWaiting = session.MatchAttempts.Any(x =>
            x.MatchingRound == session.CurrentRound &&
            x.Status == MatchAttemptStatus.Pending);

        if (stillWaiting)
            return;

        var transition = session.TryTransitionToNextRound();
        if (transition.IsError)
            return;

        await _db.SaveChangesAsync(ct);

        if (transition.Value is RoundTransitioned)
        {
            await _scheduler.EnqueueAsync(new StartMatchingRound(session.Id), ct);
        }
    }
}
```

This replaces the awkward split between:

- "matching service"
- "matching orchestrator"

There is now one explicit evaluation step.

That alone makes the workflow easier to follow.

---

## A Very Small Scheduler Abstraction

At first you do not need Wolverine yet.

You can keep Hangfire and just wrap it so the rest of your app thinks in messages instead of background-service method calls.

Example:

```csharp
public interface IMessageScheduler
{
    Task EnqueueAsync<TMessage>(TMessage message, CancellationToken ct = default);
    Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken ct = default);
}
```

Hangfire-backed implementation:

```csharp
public class HangfireMessageScheduler : IMessageScheduler
{
    public Task EnqueueAsync<TMessage>(TMessage message, CancellationToken ct = default)
    {
        BackgroundJob.Enqueue<IMessageDispatcher>(x => x.Dispatch(message, ct));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken ct = default)
    {
        BackgroundJob.Schedule<IMessageDispatcher>(x => x.Dispatch(message, ct), delay);
        return Task.CompletedTask;
    }
}
```

And one dispatcher:

```csharp
public interface IMessageDispatcher
{
    Task Dispatch<TMessage>(TMessage message, CancellationToken ct);
}

public class MediatorMessageDispatcher : IMessageDispatcher
{
    private readonly IMediator _mediator;

    public MediatorMessageDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Dispatch<TMessage>(TMessage message, CancellationToken ct)
    {
        return _mediator.Send(message!, ct);
    }
}
```

This is a really nice stepping stone because:

- your workflow starts looking message-driven
- but you do not have to adopt a new library on day one

Later, Wolverine can replace the transport and durability parts.

---

## Where Wolverine Would Fit Later

Later, the same steps could become Wolverine message handlers instead of MediatR-plus-Hangfire.

The shape stays basically the same:

```csharp
public record StartMatchingRound(MatchingSessionId MatchingSessionId);

public class StartMatchingRoundHandler
{
    public async Task Handle(
        StartMatchingRound message,
        AppDbContext db,
        IDriverRankingService ranking,
        IGeoService geo,
        IPublisher publisher,
        IMessageBus bus,
        CancellationToken ct)
    {
        // same business flow
        // load session
        // create attempts
        // save
        // publish notifications
        // schedule timeout message
    }
}
```

The important point is:

- the design idea comes first
- Wolverine is an implementation detail later

If the workflow is clear before Wolverine, adopting Wolverine becomes much easier.

---

## The Refactor Strategy I Would Use

Do not convert everything at once.

### Phase 1

Keep:

- `AppDbContext`
- MediatR
- Hangfire

Add:

- workflow message types
- message handlers
- `IMessageScheduler`

Goal:

- remove the confusing `DriverMatchingService` <-> `MatchingOrchestrator` dance

### Phase 2

Move only matching round progression to messages:

- start session
- start round
- timeout
- evaluate

Keep `AcceptMatchCommand` as-is for now.

### Phase 3

Once the matching loop feels stable, split acceptance flow:

- `Matching` marks accepted
- emits `MatchAccepted`
- `Trips` reacts and creates trip
- notifications react separately

That is the point where separate module DbContexts start making much more sense.

---

## Practical Rule Of Thumb

When you are unsure whether something is a domain event or workflow message, ask:

### Question 1

Did this happen because an aggregate changed state?

- yes -> probably a domain event
- no -> probably not

### Question 2

Am I asking the system to do a step next?

- yes -> probably a workflow message

Examples:

- `TripRequestConfirmedEvent`
  - yes, aggregate changed state
  - domain event

- `StartMatchingRound`
  - no aggregate raised it as a fact
  - we are asking the system to perform a step
  - workflow message

- `MatchingSessionCancelledEvent`
  - raised because session state changed
  - domain event

- `EvaluateMatchingProgress`
  - not a business fact
  - just a process step
  - workflow message

---

## One Simple Boundary To Remember

This sentence is the one I would keep in mind while refactoring:

> Aggregates raise domain events. The application sends workflow messages.

That one line keeps the model clean.

---

## Suggested First PR

If you want to start safely, I would make the first PR do only this:

- add `StartMatchingSession`
- add `StartMatchingRound`
- add `MatchingRoundTimedOut`
- add `EvaluateMatchingProgress`
- add `IMessageScheduler`
- change `TripRequestConfirmedEventHandler` to enqueue `StartMatchingSession`
- move round-start logic out of `DriverMatchingService`
- move round-evaluation logic out of `MatchingOrchestrator`

No module split yet.
No Wolverine yet.
No separate DbContexts yet.

Just make the process explicit.

That will already make the flow much easier to understand.

