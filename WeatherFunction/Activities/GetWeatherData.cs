using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

[DurableTask]
public class GetWeatherData
{
    private readonly HttpClient _openMeteoClient;

    public GetWeatherData(IHttpClientFactory httpClientFactory)
    {
        _openMeteoClient = httpClientFactory.CreateClient("OpenMeteo");
    }


    [Function(nameof(GetWeatherData))]
    public async Task<string> RunActivity([ActivityTrigger]  (double lat, double lon) coordinates)
    {
        // Use the injected HttpClient for OpenMeteo
        var response = await _openMeteoClient.GetAsync($"forecast?latitude={coordinates.lat}&longitude={coordinates.lon}&current_weather=true");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to fetch weather data.");
        }

        return await response.Content.ReadAsStringAsync();
    }
}
