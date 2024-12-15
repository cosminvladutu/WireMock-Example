using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WeatherFunction
{
    public class WeatherFunction
    {
        private readonly HttpClient _geocoderClient;
        private readonly HttpClient _weatherClient;

        public WeatherFunction(IHttpClientFactory httpClientFactory, ILogger<Function1> logger)
        {
            _geocoderClient = httpClientFactory.CreateClient("OpenCage");
            _weatherClient = httpClientFactory.CreateClient("OpenMeteo");

        }

        [Function("GetWeather")]
        public async Task<HttpResponseData> Run(
       [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/{city}")] HttpRequestData req,
       string city,
       FunctionContext context)
        {
            var logger = context.GetLogger("WeatherFunction");
            logger.LogInformation($"Fetching weather data for city: {city}");

            // Step 1: Get latitude and longitude for the city
            var geocodeResponse = await _geocoderClient.GetAsync($"search?q={city}");
            if (!geocodeResponse.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to fetch geocode data for city: {city}");
                var errorResponse = req.CreateResponse(geocodeResponse.StatusCode);
                await errorResponse.WriteStringAsync("City not found.");
                return errorResponse;
            }

            var geocodeContent = await geocodeResponse.Content.ReadAsStringAsync();
            var locations = JsonSerializer.Deserialize<Locations[]>(geocodeContent);

            if (locations == null || locations.Length == 0)
            {
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("City not found.");
                return notFoundResponse;
            }

            var latitude = locations[0].lat;
            var longitude = locations[0].lon;

            // Step 2: Get weather data for the latitude and longitude for the first location
            var weatherResponse = await _weatherClient.GetAsync($"forecast?latitude={latitude}&longitude={longitude}&current_weather=true");
            if (!weatherResponse.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to fetch weather data for city: {city}");
                var errorResponse = req.CreateResponse(weatherResponse.StatusCode);
                await errorResponse.WriteStringAsync("Failed to fetch weather data.");
                return errorResponse;
            }

            var weatherContent = await weatherResponse.Content.ReadAsStringAsync();

            // Step 3: Return weather data
            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            successResponse.Headers.Add("Content-Type", "application/json");
            await successResponse.WriteStringAsync(weatherContent);
            return successResponse;
        }
    }
}