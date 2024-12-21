using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WeatherFunction;
using FluentAssertions;

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

        // Act: Create a ServiceProvider with the HttpClient pointing to the mock server
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // Convert string to Uri
        });

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);  // Your function class

        var city = "Iasi";
        var result = await getCoordinates.RunActivity(city);  // Call the activity

        // Assert: Check if the returned coordinates are correct
        result.lat.Should().Be(51.321);
        result.lon.Should().Be(0.123);
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

        // Act: Create a ServiceProvider with the HttpClient pointing to the mock server
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // Convert string to Uri
        });

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);  // Your function class

        var exception = await Assert.ThrowsAsync<Exception>(() => getCoordinates.RunActivity("NonExistentCity"));

        // Assert: Exception should be thrown
        exception.Message.Should().Be("Failed to fetch geocode data for city: NonExistentCity");
    }

    [Fact]
    public async Task ShouldTimeoutAfterDelay()
    {
        // Arrange: Simulate a 3-second delay before responding
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Tokyo")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { lat = 35.6762, lon = 139.6503 })
                .WithDelay(3000));  // 3-second delay

        // Act: Create ServiceProvider
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
            client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var getCoordinates = new GetCoordinates(httpClientFactory);

        // Act: Fetch coordinates for Madrid with a header
        var result = await getCoordinates.RunActivity("Madrid");

        // Assert: Verify the result
        result.lat.Should().Be(40.4168);
        result.lon.Should().Be(-3.7038);
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
                new Location (40.7128,-74.0060)
                })); // Return array of Location objects

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
        result.lat.Should().Be(40.7128);
        result.lon.Should().Be(-74.0060);
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
                .WithBodyAsJson(new[] { new Location(51.5074, -0.1278) }));  // Return array of Location records

        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Paris")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(48.8566, 2.3522) }));  // Return array of Location records

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
        var londonResult = await getCoordinates.RunActivity("London");
        var parisResult = await getCoordinates.RunActivity("Paris");

        // Assert: Ensure the correct coordinates are returned
        londonResult.lat.Should().Be(51.5074);
        londonResult.lon.Should().Be(-0.1278);

        parisResult.lat.Should().Be(48.8566);
        parisResult.lon.Should().Be(2.3522);
    }







    public void Dispose()
    {
        // Stop WireMock server after tests
        _server.Stop();
    }
}
