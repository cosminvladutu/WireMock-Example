using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherFunction;

namespace WeatherFunction.Activities
{
    [DurableTask]
    public class GetCoordinates
    {
        private readonly HttpClient _openCageClient;

        public GetCoordinates(IHttpClientFactory httpClientFactory)
        {
            _openCageClient = httpClientFactory.CreateClient("OpenCage");
        }

        [Function(nameof(GetCoordinates))]
        public async Task<Location> RunActivity([ActivityTrigger] string city)
        {
            var response = await _openCageClient.GetAsync($"search?q={city}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch geocode data for city: {city}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var content = await response.Content.ReadAsStringAsync();
            var locations = JsonSerializer.Deserialize<Location[]>(content, options);

            // Assuming the first result is the desired location
            var latitude = locations[0].Lat;
            var longitude = locations[0].Lon;

            return new Location(latitude, longitude);
        }


    }
}