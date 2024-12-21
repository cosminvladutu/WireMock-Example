using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using WeatherFunction.Activities;

namespace WeatherFunction.Orchestrators
{
    [DurableTask]
    public class WeatherOrchestrator
    {
        [Function(nameof(WeatherOrchestrator))]
        public async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            string city = context.GetInput<string>();

            var coordinates = await context.CallActivityAsync<(double lat, double lon)>(nameof(GetCoordinates), city);
            var weatherData = await context.CallActivityAsync<string>(nameof(GetWeatherData), coordinates);

            return weatherData;
        }
    }
}