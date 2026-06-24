# Benha Scooters — Launch & Scale Plan

> Prioritized roadmap for deploying in Benha, Egypt and scaling beyond.
> Generated from a full codebase audit on 2026-03-28.

---

## Current State Summary

The backend is well-architected (Clean Architecture, DDD, CQRS, domain events) but has **critical production-readiness gaps**. The matching, trip lifecycle, wallet/commission, driver onboarding, and OTP security flows exist and work end-to-end, but many pieces are in "development-only" mode.

**What works:** Trip request → fare estimate → multi-round matching → trip lifecycle → cash payment → driver rating → wallet commission/settlement → driver onboarding/approval → OTP auth → SignalR real-time tracking → push notifications (Firebase).

**What doesn't exist yet:** Geofencing, surge pricing, cancellation penalties, online payments, ride scheduling, driver-rider chat, proper observability, load testing.

---

## Phase 0 — Security & Credential Hardening (BLOCKERS)

These **must** be resolved before any real user touches the system.

| # | Item | Severity | Details |
|---|------|----------|---------|
| 0.1 | **Remove secrets from appsettings.json** | 🔴 Critical | Neon DB password, Google OAuth client IDs, S3 keys are all committed to git. Move everything to User Secrets / env vars / a vault. Rotate all compromised credentials. |
| 0.2 | **Disable fixed OTP in production** | 🔴 Critical | `UseFixedOtp: true` with code `123456` is the current default. Anyone can bypass auth. Must be `false` in production config. |
| 0.3 | **Fix JWT token lifetime** | 🔴 Critical | Access token lifetime is set to **1,000,000 minutes (~694 days)**. Set to 15–30 minutes for production. |
| 0.4 | **Fix hardcoded admin UserId** | 🔴 Critical | `ApproveDriver` uses `UserId.Create(1)` instead of the actual authenticated admin. Replace with claims from `HttpContext.User`. |
| 0.5 | **Fix HMAC key** | 🔴 Critical | OTP HMAC key is a placeholder string. Generate a proper 64-char hex secret for production. |
| 0.6 | **Enforce HTTPS** | 🟡 High | Docker config has no TLS termination. Add a reverse proxy (Caddy/nginx) with Let's Encrypt, or use a cloud load balancer. |

---

## Phase 1 — Production Readiness (Pre-Launch)

### 1A. Benha-Specific Configuration

| # | Item | Why | Effort |
|---|------|-----|--------|
| 1.1 | **Add geofencing / service area** | Currently accepts ride requests from anywhere on Earth. Define a polygon around Benha (~15 km²) and reject trips outside it. A simple bounding-box check on pickup & dropoff coordinates is enough to start. | Small |
| 1.2 | **Tune fare pricing for Benha** | Base fare 17 EGP + 5 EGP/km + 0.10 EGP/min was set without market research. Benchmark against Benha tuk-tuk / motorcycle taxi fares. A 3 km ride currently costs ~32 EGP; validate this is competitive. | Config |
| 1.3 | **Tune matching radius** | Base radius 3 km with +500m/round is designed for a larger city. Benha is ~5 km across. In round 3 you'd search 4.5 km which covers the whole city — this may be fine, but consider starting at 1.5 km for faster matches in the congested center. | Config |
| 1.4 | **Tune round timeouts** | 180 seconds/round × 3 rounds = up to 9 minutes before a rider is told "no drivers." In a small city this is too long. Consider 60s/round to fail fast. | Config |
| 1.5 | **Test coordinates are New York** | `lats-lngs.md` has NYC test data. Replace with Benha landmarks (train station, university, hospitals, souqs) for QA and demo. | Small |
| 1.6 | **Arabic API error messages** | Domain validation errors are in mixed Arabic/English. Standardize all user-facing messages to Arabic. Backend error codes stay English. | Medium |

### 1B. Data & Performance

| # | Item | Why | Effort |
|---|------|-----|--------|
| 1.7 | **Add database indexes** | Missing indexes on: `DriverLocation(UserId, UpdatedAt)`, `DriverMatchAttempts(DriverUserId, Status)`, `SmsVerificationCodes(UserId, CreatedAt)`, `RefreshTokens(ExpiresAt)`, `OtpSecurityEvents(UserId, CreatedAt)`, `Trips(DriverUserId, Status)`, `Trips(RiderUserId, Status)`. | Small |
| 1.8 | **Fix N+1 in SignalR hubs** | `DriverHub.SendPendingOffersAsync()` does 4-level nested Includes for every reconnect. Use a projection query returning only DTOs. | Medium |
| 1.9 | **Add data retention policies** | `TripGpsPoints`, `OtpSecurityEvents`, `SmsLogs`, `DriverLocation` history all grow unbounded. Add cleanup jobs: GPS points archived or deleted after 90 days, security events after 180 days. | Medium |
| 1.10 | **Connection pooling** | No explicit pooling config. For Neon/remote PostgreSQL, configure `MaxPoolSize`, `MinPoolSize`, and `Connection Idle Lifetime` in the connection string. | Small |

### 1C. Reliability

| # | Item | Why | Effort |
|---|------|-----|--------|
| 1.11 | **Optimistic concurrency on Trip & Wallet** | No `RowVersion` / concurrency tokens. Two drivers could race to accept the same match, or concurrent wallet charges could corrupt the balance. Add EF Core concurrency tokens to `Trip`, `MatchingSession`, `DriverWallet`. | Medium |
| 1.12 | **Idempotent match acceptance** | If a driver taps "Accept" twice quickly, the system should not create two trips. Add an idempotency check (e.g., if session already has an accepted match, return the existing trip). | Small |
| 1.13 | **SMS retry with backoff** | `WhySmsSenderService` has no retry logic. If the SMS API is temporarily down, the user gets no OTP and no feedback. Add Polly retry (2 attempts, exponential backoff). | Small |
| 1.14 | **Health checks** | No `/health` endpoint. Add ASP.NET Core health checks for: DB connectivity, Hangfire, S3/MinIO, Firebase. Essential for container orchestration. | Small |

### 1D. Testing

| # | Item | Why | Effort |
|---|------|-----|--------|
| 1.15 | **Integration tests for trip lifecycle** | The full flow (request → match → accept → arrive → start → complete → pay → rate) has zero test coverage. Write 1 happy-path end-to-end test. | Medium |
| 1.16 | **Unit tests for wallet** | Commission charging, debt limit enforcement, settlement, refund on cancellation — all untested. | Medium |
| 1.17 | **Unit tests for OTP security** | Brute-force lockout, rate limiting, fraud detection — critical auth path with no tests. | Medium |
| 1.18 | **Load test the matching flow** | Simulate 50 concurrent trip requests with 100 online drivers. Identify bottlenecks before real traffic hits. | Medium |

---

## Phase 2 — Launch-Day Features (Week 1–2 of Operation)

These aren't blockers but will be needed almost immediately once drivers and riders start using the app.

| # | Item | Why | Effort |
|---|------|-----|--------|
| 2.1 | **Cancellation penalties** | Currently riders and drivers can cancel freely with no consequences. Add: rider cancellation fee after driver is assigned (e.g., 5 EGP); driver cancellation penalty (lower priority in matching, or wallet charge after N cancels). | Medium |
| 2.2 | **Driver arrival verification** | Driver can mark "arrived" from anywhere. Add a proximity check: driver must be within 200m of pickup to mark arrival. Prevents gaming. | Small |
| 2.3 | **Trip completion verification** | Driver can mark "completed" without actually reaching the dropoff. Add a proximity check to dropoff coordinates. | Small |
| 2.4 | **Rider-rates-trip notification** | `TripCompletedEventHandler` notifies the rider, but the rating prompt is passive. Add a push notification 5 minutes after completion asking for a rating. | Small |
| 2.5 | **Driver suspension on low rating** | No automatic action when a driver's rating drops below a threshold (e.g., 3.0). Add auto-suspension at < 3.0 after a minimum of 10 trips. | Small |
| 2.6 | **Ride-in-progress safety** | No "Share my ride" or emergency button. For Benha (congested, narrow streets), add an emergency contact notification feature that shares live trip location. | Medium |
| 2.7 | **Estimated pickup time for rider** | After matching, the rider gets driver info but no ETA. The `EstimatedArrivalTime` is calculated during matching — surface it to the rider via SignalR. | Small |
| 2.8 | **Password policy enforcement** | Password regex is commented out. Minimum 8 chars with complexity is enforced nowhere. Re-enable it. | Small |

---

## Phase 3 — Operational Excellence (Month 1–2)

| # | Item | Why | Effort |
|---|------|-----|--------|
| 3.1 | **Observability stack** | No structured logging correlation IDs, no distributed tracing, no metrics. Add: Serilog enrichers with `CorrelationId` per request, OpenTelemetry traces on matching + trip flows, Prometheus metrics for active trips / online drivers / match success rate. | Large |
| 3.2 | **Distributed rate limiting** | OTP rate limiter uses `IMemoryCache` — won't work with multiple server instances. Swap to Redis-backed rate limiting. | Medium |
| 3.3 | **Automated wallet settlement** | Currently every top-up requires manual admin review of a receipt image. For scaling, integrate with a payment gateway (Fawry, Paymob, or Vodafone Cash) for instant wallet top-up. | Large |
| 3.4 | **Surge pricing** | No dynamic pricing. During peak hours (school commute 7–8 AM, evening 5–7 PM) in congested Benha, demand will spike. Add a multiplier based on the ratio of pending requests to online drivers in the area. | Medium |
| 3.5 | **Driver earnings dashboard** | Drivers need to see: today's trips, earnings, commission deducted, current balance. This is a read-model / query — expose via API (the mobile app can render it). | Medium |
| 3.6 | **Trip receipt / invoice** | After completion, generate a simple receipt (rider name, driver name, route, fare breakdown, payment method). Required for rider trust and potential regulatory compliance. | Medium |
| 3.7 | **CORS policy** | `app.UseCors()` is called without any configuration — defaults to allow all origins. Lock it down to your mobile app's domain and admin panel. | Small |
| 3.8 | **API versioning** | No versioning strategy. When the mobile app ships, you can't break the API. Add `/api/v1/` prefix or header-based versioning before the first public release. | Medium |

---

## Phase 4 — Scaling Beyond Benha

If the service succeeds in Benha (population ~200K), here's what needs to change to expand to neighboring cities or Cairo suburbs.

### Infrastructure Scaling

| # | Item | Why |
|---|------|-----|
| 4.1 | **Move to managed hosting** | Dockerized single-server deployment won't handle Cairo traffic. Move to a cloud provider (AWS ECS/EKS, Azure Container Apps, or Hetzner + K3s for cost efficiency). |
| 4.2 | **PostgreSQL read replicas** | All queries and writes go to a single DB. Add a read replica for queries (trip history, driver stats, wallet transactions). Use EF Core's `UseNpgsql` with read/write splitting. |
| 4.3 | **Redis for caching & rate limiting** | Replace all `IMemoryCache` usage. Cache: driver locations (TTL 30s), fare estimates (TTL 5 min), session data. Rate limit: OTP, API endpoints. Pub/Sub for SignalR backplane. |
| 4.4 | **SignalR backplane** | With multiple server instances, SignalR needs a backplane (Redis). Without it, a rider connected to server A won't receive messages from server B. |
| 4.5 | **Separate Hangfire worker** | Background jobs (matching timeouts, cleanup) should run on a dedicated worker process, not the API server. Prevents job processing from competing with request handling. |
| 4.6 | **Object storage migration** | `LocalS3Service` stores files on disk. Move to real S3 or a managed MinIO cluster. |
| 4.7 | **CDN for static assets** | Driver photos, documents, receipts served from app server. Put behind Cloudflare or similar CDN. |

### Application Scaling

| # | Item | Why |
|---|------|-----|
| 4.8 | **Multi-city / zone support** | Currently no concept of "city" or "zone." Add a `ServiceArea` entity with polygon boundaries, city-specific pricing, and separate driver pools. |
| 4.9 | **Online payments** | Cash-only doesn't scale. Integrate Paymob or Fawry for card payments and mobile wallets (Vodafone Cash, Orange Money, Instapay). |
| 4.10 | **Scheduled rides** | In a larger market, riders want to book a ride for tomorrow morning. Add a `ScheduledTripRequest` with a pickup time and trigger matching N minutes before. |
| 4.11 | **Driver-rider in-app chat** | Reduce phone call dependency. Add a simple text messaging channel (can use SignalR, already in place). |
| 4.12 | **Referral / promo codes** | For growth in new cities. Add a `PromoCode` entity with discount rules, usage limits, and expiry. |
| 4.13 | **Event sourcing for wallet** | The wallet currently stores a running balance + transaction log. For financial auditability at scale, consider event-sourcing the wallet (balance derived from events, never directly mutated). |
| 4.14 | **Analytics pipeline** | Trip data, GPS traces, matching success rates — all valuable. Export to a data warehouse (BigQuery / ClickHouse) for BI dashboards and operational insights. |

---

## Technical Debt Register

Existing issues in the codebase that should be cleaned up progressively.

| # | Location | Issue |
|---|----------|-------|
| D1 | `Trip.cs` | `TripRequest?` is nullable with a `// TODO: this is optional for now to fix the migration` comment. Fix the schema properly. |
| D2 | `GetAvailableTripsQuery`, `GetDriverMatchOffers` | 4 instances of `// TODO: Future - Update DTO to use Distance/Duration value object`. Complete the refactor. |
| D3 | `HungTripCleanupService` | `// TODO: still doesn't solve the problem of trips that are hung`. Consider adding driver pings — if driver doesn't send a location update for 30 min during a trip, auto-warn and then cancel. |
| D4 | `MatchingCleanupService` | Deletes match attempts after 24 hours. No archive — lost for analytics. Archive to a separate table or export before deletion. |
| D5 | `OtpSecurityService` | Monolithic class handling: rate limiting, fraud detection, verification, logging. Split into `IOtpRateLimiter`, `IFraudDetector`, `IOtpVerifier`. |
| D6 | Refresh tokens | Stored as plaintext in DB. Hash them (SHA-256) so a DB breach doesn't compromise sessions. |
| D7 | SignalR JWT via query string | Tokens appear in HTTP access logs and proxy logs. Use a short-lived negotiation token pattern instead. |
| D8 | `DriverRankingService` | Scoring is 70% distance, 30% rating. No factor for: completion rate, acceptance rate, recency of location update. Improve the algorithm as data accumulates. |
| D9 | Email verification | `EmailVerified` claim exists but no verification flow is implemented. Either implement it or remove the claim. |
| D10 | Password validation | Regex is commented out in `ValidationRegex`. Decide on a policy and enforce it. |

---

## Benha-Specific Considerations

Things to watch out for that are unique to operating in a small, congested Egyptian city:

1. **Narrow streets & GPS drift** — Benha has many narrow alleys where GPS accuracy drops to 15–30m. The matching radius and driver-arrival check must account for this. Don't make the arrival proximity check (Phase 2.2) too strict (200m is okay; 50m would fail often).

2. **Scooter/tuk-tuk norms** — Verify `VehicleType` enum covers the actual vehicle types used in Benha (motorcycles, scooters, tuk-tuks). The license plate regex `^[أ-ي]{3}\s?\d{4}$` may not match motorcycle plates, which have different formats.

3. **Network quality** — Mobile data in Benha can be spotty. The matching timeout of 180s needs to tolerate slow connections. Ensure push notifications have a fallback (polling endpoint for pending match offers — check `GetDriverMatchOffersQuery` is accessible).

4. **Cash-dominated economy** — Online payments are a growth feature, not a launch requirement. Cash is the default and should work flawlessly. The wallet commission system (driver owes platform % of each fare, settles periodically) is the right model for a cash market.

5. **Driver trust** — Commission rate (10%) and debt limit (69 EGP ≈ ~7 trips before lockout) need validation with real drivers. If too aggressive, drivers won't onboard. Consider a lower commission (5–7%) for launch to build supply.

6. **Peak congestion** — Benha's main commercial streets (Farid Nada St, railway crossing areas) get severely congested. Google Maps ETAs will reflect this, but the fare estimate (0.10 EGP/min) barely penalizes time. In a 30-min traffic jam, the extra charge is only 3 EGP. Consider raising the per-minute rate or adding a minimum fare.

7. **Regulatory** — Check if Benha / Qalyubia Governorate requires ride-hailing licenses. Egypt's ride-hailing law (2018) primarily covers TNC platforms; scooter services may fall in a gray area. Get legal advice before launch.

---

## Priority Summary

```
NOW (before any user):     Phase 0 — all 6 items
Week 1-2 of development:   Phase 1A (Benha config) + Phase 1B (indexes, N+1)
Week 2-3:                  Phase 1C (concurrency, retries) + Phase 1D (tests)
Launch day:                Phase 2.1–2.3 (cancellation, arrival/completion checks)
Month 1:                   Phase 2.4–2.8 + Phase 3.1–3.2
Month 2-3:                 Phase 3.3–3.8
When expanding:            Phase 4
Ongoing:                   Technical debt register
```
