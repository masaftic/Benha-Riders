using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BenhaScooters.Contracts.Matching;
using BenhaScooters.Contracts.TripRequests;
using BenhaScooters.Contracts.Trips;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.IntegrationTests.Fixtures;
using BenhaScooters.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.IntegrationTests.Tests;

[Collection("db")]
public class TripLifecycleFeatureTests(TestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task FullTripLifecycle_FromRequestToRating_ShouldSucceedEndToEnd()
    {
        // 1. Arrange & Seed: verified rider and approved driver located near Benha Station
        var seeder = GetRequiredService<TestAccountSeeder>();
        await TestGeoFixtures.SeedBenhaServiceAreaAsync(DbContext);

        var rider = await seeder.CreateVerifiedRiderAsync(email: "e2e_rider@test.local", phoneNumber: "+201099990001");
        var driver = await seeder.CreateApprovedDriverAsync(
            email: "e2e_driver@test.local",
            phoneNumber: "+201099990002",
            nationalId: "29901011234567",
            licensePlate: "BEN1234",
            location: TestGeoFixtures.BenhaStation);

        // 2. Rider requests a trip inside Benha service area
        SetAuthorizationHeader(rider.AccessToken);
        var requestPayload = new RequestTripRequest(
            TestGeoFixtures.BenhaStation.Lat,
            TestGeoFixtures.BenhaStation.Lng,
            TestGeoFixtures.BenhaUniversity.Lat,
            TestGeoFixtures.BenhaUniversity.Lng,
            "Faculty of Engineering, Benha University");

        var requestResponse = await Client.PostAsJsonAsync("/api/trip-requests", requestPayload, JsonOptions);
        requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var requestJson = await DeserializeResponse<JsonElement>(requestResponse);
        var tripRequestId = requestJson.GetProperty("tripRequestId").GetInt32();
        tripRequestId.Should().BeGreaterThan(0);

        // 3. Rider confirms trip request -> schedules matching session and round
        var confirmResponse = await Client.PostAsync($"/api/trip-requests/{tripRequestId}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Process enqueued matching workflow jobs deterministically in test host
        var scheduler = GetRequiredService<RecordingMessageScheduler>();
        var sender = GetRequiredService<MediatR.ISender>();
        await scheduler.DrainImmediateAsync(sender);

        // 4. Driver checks for pending match offers
        SetAuthorizationHeader(driver.AccessToken);
        var offersResponse = await Client.GetAsync("/api/matching/offers");
        offersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var offersJson = await DeserializeResponse<JsonElement>(offersResponse);
        var offersArray = offersJson.GetProperty("matchOffers");
        offersArray.GetArrayLength().Should().BeGreaterThan(0);

        var attemptId = offersArray[0].GetProperty("driverMatchAttemptId").GetInt32();

        // 5. Driver accepts the offer -> Trip is created
        var acceptPayload = new AcceptMatchRequest(attemptId);
        var acceptResponse = await Client.PostAsJsonAsync("/api/matching/accept", acceptPayload, JsonOptions);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptJson = await DeserializeResponse<JsonElement>(acceptResponse);
        var tripId = acceptJson.GetProperty("tripId").GetInt32();
        tripId.Should().BeGreaterThan(0);

        // Verify driver transitioned to OnTrip status
        var driverStatus = await DbContext.DriverStatuses.AsNoTracking().FirstAsync(ds => ds.UserId == driver.User.Id);
        driverStatus.Status.Should().Be(DriverAvailabilityStatus.OnTrip);

        // 6. Driver marks arrived at pickup
        var arrivedResponse = await Client.PostAsync($"/api/trips/{tripId}/arrived", null);
        arrivedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tripAfterArrive = await DbContext.Trips.AsNoTracking().FirstAsync(t => t.Id == TripId.Create(tripId));
        tripAfterArrive.Status.Should().Be(TripStatus.DriverArrived);

        // 7. Driver starts trip -> InProgress
        var startResponse = await Client.PostAsync($"/api/trips/{tripId}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tripAfterStart = await DbContext.Trips.AsNoTracking().FirstAsync(t => t.Id == TripId.Create(tripId));
        tripAfterStart.Status.Should().Be(TripStatus.InProgress);

        // 8. Driver completes trip -> Completed and payment recorded
        var completeResponse = await Client.PostAsync($"/api/trips/{tripId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tripAfterComplete = await DbContext.Trips.AsNoTracking().FirstAsync(t => t.Id == TripId.Create(tripId));
        tripAfterComplete.Status.Should().Be(TripStatus.Completed);

        // 9. Rider rates the driver
        SetAuthorizationHeader(rider.AccessToken);
        var ratePayload = new RateDriverRequest(5, "Excellent and safe ride!");
        var rateResponse = await Client.PostAsJsonAsync($"/api/trips/{tripId}/rate", ratePayload, JsonOptions);
        rateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify TripRating was persisted
        var rating = await DbContext.TripRatings.AsNoTracking().FirstOrDefaultAsync(r => r.TripId == TripId.Create(tripId));
        rating.Should().NotBeNull();
        rating!.DriverRating.Should().Be(5);
        rating.DriverComment.Should().Be("Excellent and safe ride!");
    }
}
