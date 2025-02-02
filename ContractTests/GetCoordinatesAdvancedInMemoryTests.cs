using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WeatherFunction;
using FluentAssertions;
using WeatherFunction.Activities;
using WireMock.Settings;

public class GetCoordinatesAdvancedInMemoryTests : IDisposable
{
    private readonly WireMockServer _server;

    public GetCoordinatesAdvancedInMemoryTests()
    {
        // Configure WireMock server to store mappings in memory
        var settings = new WireMockServerSettings
        {
            // Enable in-memory storage
            FileSystemHandler = null, // Disable file system storage
            ProxyAndRecordSettings = new ProxyAndRecordSettings
            {
                SaveMapping = true, // This will save the mappings to memory
                Url = "https://geocode.maps.co", // External API you're proxying to
            }
        };

        _server = WireMockServer.Start(settings);
    }

    [Fact]
    public async Task ShouldReturnLatLon_For_IasiCity()
    {
        // Arrange: Set up mock response for Iasi city query
        _server.Given(Request.Create()
                .WithPath("/search")
                .WithParam("q", "Iasi")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[] { new Location(51.321, 0.123) })); // Mocked response for Iasi

        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // WireMock server URL
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var getCoordinates = new GetCoordinates(httpClientFactory);  // SUT
        var city = "Iasi";

        // Act: Send the request to WireMock server
        var result = await getCoordinates.RunActivity(city);

        // Assert: Verify the result
        result.lat.Should().Be(51.321); 
        result.lon.Should().Be(0.123);  

        // Optionally, check if the request was logged in memory
        var logEntries = _server.LogEntries; // Access log entries directly
        logEntries.Should().ContainSingle(); // Ensure the request was logged
    }

    [Fact]
    public async Task ShouldReplayRecordedResponse_FromMemory_For_IasiCity()
    {
        // Arrange: Set up mock response for Iasi city query (this simulates the initial recording)
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // WireMock server URL (acts as proxy)
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var getCoordinates = new GetCoordinates(httpClientFactory);  // SUT
        var city = "Iasi";

        // Act: Send the request to WireMock server, which will proxy to the actual API and save response in memory
        var result = await getCoordinates.RunActivity(city);

        // Assert: Verify the result
        result.lat.Should().Be(47.1615598);
        result.lon.Should().Be(27.5837814);

        // Now, make the same request again (replay)
        var resultReplay = await getCoordinates.RunActivity(city);

        // Assert: Verify the replayed result (should match the original response)
        result.lat.Should().Be(47.1615598);
        result.lon.Should().Be(27.5837814);
    }

    public void Dispose()
    {
        // Stop WireMock server after tests
        _server.Stop();
    }
}
