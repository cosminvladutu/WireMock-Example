using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace WeatherFunction.Activities
{
    [DurableTask]
    public class GetWeatherData
    {
        private readonly HttpClient _openMeteoClient;

        public GetWeatherData(IHttpClientFactory httpClientFactory)
        {
            _openMeteoClient = httpClientFactory.CreateClient("OpenMeteo");
        }


        [Function(nameof(GetWeatherData))]
        public async Task<string> RunActivity([ActivityTrigger] Location coordinates)
        {
            // Use the injected HttpClient for OpenMeteo
            var response = await _openMeteoClient.GetAsync($"forecast?latitude={coordinates.Lat}&longitude={coordinates.Lon}&current_weather=true");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch weather data.");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}