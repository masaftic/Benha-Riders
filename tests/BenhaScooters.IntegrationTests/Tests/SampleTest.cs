using System.Net;
using BenhaScooters.IntegrationTests.Fixtures;
using FluentAssertions;

namespace BenhaScooters.IntegrationTests.Tests;

[Collection("db")]
public class SampleTest(TestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Test1()
    {
        var response = await Client.GetAsync("/api/auth/test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test2()
    {
        var response = await Client.GetAsync("/api/auth/test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test3()
    {
        var response = await Client.GetAsync("/api/auth/test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
