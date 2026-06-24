# FCM Translation Plan

## Goal

Begin translating FCM push notifications in a consistent way without scattering hard-coded Arabic and English strings across handlers and commands.

## Current FCM Usage

The current FCM entry point is:

- `src/BenhaScooters/Application/Abstractions/IPushNotificationService.cs`
- `src/BenhaScooters/Infrastructure/Notifications/FirebasePushNotificationService.cs`

Current `SendToUserAsync(...)` call sites:

1. `src/BenhaScooters/Application/Features/DriverOnboarding/Commands/ApproveDriver.cs`
   Current event: driver approval
   Current status: now Arabic

2. `src/BenhaScooters/Application/Features/Matching/Commands/AcceptMatchCommand.cs`
   Event: rider is told a driver has been assigned
   Current status: Arabic

3. `src/BenhaScooters/Application/Features/Matching/EventHandlers/MatchingSessionCancelledEventHandler.cs`
   Event: rider is told no driver was found / trip request canceled
   Current status: Arabic

4. `src/BenhaScooters/Application/Features/Matching/EventHandlers/DriverMatchOfferCreatedEventHandler.cs`
   Event: driver receives a ride offer
   Current status: mixed
   Note: title/body are Arabic, but fallback payload values still include English text like `Unknown pickup location`

5. `src/BenhaScooters/Application/Features/Trips/EventHandlers/DriverArrivedEventHandler.cs`
   Event: rider is told the driver arrived
   Current status: Arabic

6. `src/BenhaScooters/Application/Features/Trips/EventHandlers/TripCancelledEventHandler.cs`
   Event: rider or driver is told a trip was canceled
   Current status: Arabic

## What The Code Is Doing Today

- Push titles and bodies are passed as raw strings directly at each call site.
- SignalR notifications are separate from FCM notifications.
- There is already localization infrastructure in the repo for API resources:
  - `src/BenhaScooters/Resources/Presentation/Localization/ApiErrorResources.en.resx`
  - `src/BenhaScooters/Resources/Presentation/Localization/ApiErrorResources.ar.resx`
- FCM notifications are not yet using a centralized localization layer.

## Recommended First Steps

1. Inventory every push notification type.
   Start with the `type` payload values already in use:
   - `driver_approved`
   - `trip_assigned`
   - `trip_request_canceled`
   - `ride_request_offer`
   - `driver_arrived`
   - `trip_cancelled`

2. Centralize push message creation.
   Add a small notification-message builder or factory so handlers pass structured data instead of raw Arabic/English strings.

3. Base language selection on the user.
   Reuse the user preferred language flow already present in the repo and resolve the language before building push content.

4. Move push strings into localization resources.
   Create dedicated resource keys for push titles and bodies instead of hard-coding them inside handlers.

5. Translate payload fallback strings too.
   Example: `Unknown pickup location`, `Unknown dropoff location`, `Driver`, `Unknown`.

6. Keep payload `type` values stable.
   Translate human-visible text only; do not translate machine-readable payload keys like `type`, `tripId`, or `matchAttemptId`.

## Suggested Implementation Shape

Create something like:

- `Application/Notifications/PushNotificationMessageFactory.cs`
- Or `Infrastructure/Notifications/LocalizedPushNotificationFactory.cs`

That factory would return:

- `Title`
- `Body`
- Optional `Data`

Inputs would be:

- notification type
- user language
- event-specific values such as driver name, fare, license plate, or cancellation reason

## Good Migration Order

1. Start with the current call sites only.
2. Replace hard-coded strings one handler at a time.
3. Add Arabic and English resource entries for each push type.
4. Verify one driver flow and one rider flow end to end.
5. After that, expand the same pattern to SignalR user-facing text if needed.

## Small Risks To Watch

- Mixed-language payloads if title/body are translated but fallback data strings are not.
- Duplicating the same text in both SignalR and FCM code paths.
- Future contributors adding new hard-coded strings unless there is one obvious factory/service to use.

## Practical Next Task

A good next implementation step is:

- add a centralized localized push-message builder
- move the existing six push notification call sites to it
- source language from the user preferred-language setting
