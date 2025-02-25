﻿using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WeatherFunction;
using FluentAssertions;
using WeatherFunction.Activities;

public class GetCoordinatesTests
{
    private readonly WireMockServer _server;

    public GetCoordinatesTests()
    {
        // Start WireMock server
        _server = WireMockServer.Start();
    }

    [Fact]
    public async Task ShouldReturnLatLon_For_IasiCity()
    {
        // Arrange: Set up mock response for OpenCage API
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Iasi")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[]
                {
                    new Location (51.321, 0.123)
                }));

        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // Convert string to Uri
        });

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // ACT
        var getCoordinates = new GetCoordinates(httpClientFactory);  // Create SUT

        var city = "Iasi";

        var result = await getCoordinates.RunActivity(city);

        // Assert
        result.Lat.Should().Be(51.321);
        result.Lon.Should().Be(0.123);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForInvalidCity()
    {
        // Arrange: Set up mock response for OpenCage API
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "NonExistentCity")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("City not found"));

        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        var exception = await Assert.ThrowsAsync<Exception>(() => getCoordinates.RunActivity("NonExistentCity"));
        exception.Message.Should().Be("Failed to fetch geocode data for city: NonExistentCity");
    }

    [Fact]
    public async Task ShouldTimeoutAfterDelay()
    {
        // Arrange: SimuLate a 3-second delay before responding
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Tokyo")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { Lat = 35.6762, Lon = 139.6503 })
                .WithDelay(3000));  // 3-second delay

        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
            client.Timeout = TimeSpan.FromSeconds(2);  // Set timeout less than the delay
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Try to fetch coordinates and expect a timeout exception
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => getCoordinates.RunActivity("Tokyo"));

        // Assert: Exception should be due to timeout
        exception.Message.Should().Contain("The request was canceled due to the configured HttpClient.Timeout of 2 seconds elapsing.");
    }


    [Fact]
    public async Task ShouldHandleDifferentStatusCodes()
    {
        // Arrange: Mock 500 Internal Server Error
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Berlin")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal server error"));

        // Act: Create ServiceProvider
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Call the function and expect an exception
        var exception = await Assert.ThrowsAsync<Exception>(() => getCoordinates.RunActivity("Berlin"));

        // Assert: Exception should be thrown due to the 500 status
        exception.Message.Should().Be("Failed to fetch geocode data for city: Berlin");
    }

    [Fact]
    public async Task ShouldHandleRequestsWithHeaders()
    {
        // Arrange: Set up WireMock with request headers
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Madrid")
                .WithHeader("Authorization", "Bearer test-token")  // Mock header
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[]
                {
                new Location (40.4168, -3.7038) // Mock response for Madrid coordinates
                }));

        // Act: Create ServiceProvider and inject HttpClient
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token"); // Add the header
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for Madrid with a header
        var result = await getCoordinates.RunActivity("Madrid");

        // Assert: Verify the result
        result.Lat.Should().Be(40.4168);
        result.Lon.Should().Be(-3.7038);
    }


    [Fact]
    public async Task ShouldHandleQueryParametersCorrectly()
    {
        // Arrange: Mock request for different city
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "New York")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[]
                {
                new Location (40.7128,-74.0060),
                new Location (42,-74)
                }));

        // Act: Create ServiceProvider
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for New York
        var result = await getCoordinates.RunActivity("New York");

        // Assert: Ensure the coordinates are correct
        result.Lat.Should().Be(40.7128);
        result.Lon.Should().Be(-74.0060);
    }


    [Fact]
    public async Task ShouldReturnDifferentResultsBasedOnQueryParameter()
    {
        // Arrange: Mock two responses for different cities
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "London")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(51.5074, -0.1278) }));

        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Paris")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(48.8566, 2.3522) }));

        // Act: Create ServiceProvider
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for London and Paris
        var LondonResult = await getCoordinates.RunActivity("London");
        var parisResult = await getCoordinates.RunActivity("Paris");

        // Assert: Ensure the correct coordinates are returned
        LondonResult.Lat.Should().Be(51.5074);
        LondonResult.Lon.Should().Be(-0.1278);

        parisResult.Lat.Should().Be(48.8566);
        parisResult.Lon.Should().Be(2.3522);
    }

    [Fact]
    public async Task ShouldVerifyRequests()
    {
        // Arrange: Mock two responses for different cities
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "London")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(51.5074, -0.1278) }));

        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Paris")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(48.8566, 2.3522) }));

        // Act: Create ServiceProvider
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for London and Paris
        await getCoordinates.RunActivity("London");
        await getCoordinates.RunActivity("Paris");

        // Assert
        _server.LogEntries.Should().HaveCount(2);
        _server.LogEntries.Should().Contain(s => s.RequestMessage.Path == "/search" && s.RequestMessage.Method == "GET");
        _server.LogEntries.Count(entry => entry.RequestMessage.Query["q"].Contains("London")).Should().Be(1);
        _server.LogEntries.Count(entry => entry.RequestMessage.Query["q"].Contains("Paris")).Should().Be(1);
    }

    [Fact]
    public async Task ShouldVerifyRequestsV2()
    {
        // Arrange: Mock two responses for different cities
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "London")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(51.5074, -0.1278) }));

        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Paris")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(48.8566, 2.3522) }));

        // Act: Create ServiceProvider
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for London and Paris
        await getCoordinates.RunActivity("London");
        await getCoordinates.RunActivity("Paris");

        // Assert
        // Find requests with specific query parameters
        var LondonRequests = _server.FindLogEntries(Request.Create()
            .WithPath("/search")
            .WithParam("q", "London"));

        var parisRequests = _server.FindLogEntries(Request.Create()
            .WithPath("/search")
            .WithParam("q", "Paris"));

        LondonRequests.Should().NotBeNull();
        LondonRequests.Should().HaveCount(1);
        LondonRequests.First().RequestMessage.Query["q"].Should().Contain("London");

        parisRequests.Should().NotBeNull();
        parisRequests.Should().HaveCount(1);
        parisRequests.First().RequestMessage.Query["q"].Should().Contain("Paris");
    }

    public void Dispose()
    {
        // Stop WireMock server after tests
        _server.Stop();
    }
}
