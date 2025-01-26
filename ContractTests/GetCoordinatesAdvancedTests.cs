using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WeatherFunction;
using FluentAssertions;
using WeatherFunction.Activities;
using WireMock.Settings;
using System;
using System.IO;
using System.Threading.Tasks;
using WireMock.Handlers;

public class GetCoordinatesAdvancedTests : IDisposable
{
    private readonly WireMockServer _server;

    public GetCoordinatesAdvancedTests()
    {
        // Ensure the directory exists for saving mappings
        var directoryPath = @"C:\PoCs\WireMock-Example\map";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Start WireMock server with Proxy and Record settings
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            // Enable proxy and record
            ProxyAndRecordSettings = new ProxyAndRecordSettings
            {
                // Set proxy URL to forward requests to
                Url = "https://geocode.maps.co/",  // Proxy target API URL
                SaveMapping = true,
                SaveMappingToFile = true
            },
            FileSystemHandler = new LocalFileSystemHandler(directoryPath) // Directory to save mappings
        });
    }

    [Fact]
    public async Task ShouldReturnLatLon_For_IasiCity()
    {
        // Arrange: Set up WireMock server to record requests and forward to external API
        var services = new ServiceCollection();
        services.AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri(_server.Url);  // WireMock server URL (acts as proxy)
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var getCoordinates = new GetCoordinates(httpClientFactory);  // SUT
        var city = "Iasi";

        // Act: Send the request to WireMock server, which will proxy to the actual API
        var result = await getCoordinates.RunActivity(city);

        // Assert: Verify the result
        //result.lat.Should().Be(51.321);  // Check expected latitude
        //result.lon.Should().Be(0.123);  // Check expected longitude

        // Optionally, check if the request was logged
        var logEntries = _server.FindLogEntries(Request.Create().WithPath("/search"));
        logEntries.Should().ContainSingle(); // Ensure the request was logged
    }


    [Fact]
    public async Task ShouldReplayRecordedResponse_For_IasiCity()
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

        // Act: Send the request to WireMock server, which will proxy to the actual API and save response
        var result = await getCoordinates.RunActivity(city);

        // Verify that the mapping was saved
        var mappingFilePath = @"C:\PoCs\WireMock-Example\map\__admin\mappings\Proxy Mapping for _GET_search.json"; // Adjust this path if necessary
        System.IO.File.Exists(mappingFilePath).Should().BeTrue(); // Check if the mapping file exists

        // Assert: Verify the result
        result.lat.Should().Be(0);
        result.lon.Should().Be(0);

        // Now, make the same request again (replay)
        var resultReplay = await getCoordinates.RunActivity(city);

        // Assert: Verify the replayed result (should match the original response)
        resultReplay.lat.Should().Be(0);
        resultReplay.lon.Should().Be(0);
    }

    public void Dispose()
    {
        // Stop WireMock server after tests
        _server.Stop();
    }
}
