using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.DurableTask.Client;
using WeatherFunction.Orchestrators;

namespace WeatherFunction.Triggers
{

    // HTTP trigger function to start the orchestration
    public class GetWeatherHttpTrigger
    {
        [Function("GetWeather")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/{city}")] HttpRequestData req,
            string city,
            [DurableClient] DurableTaskClient starter)
        {
            // Start the orchestrator
            var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WeatherOrchestrator), city);

            // Return the orchestration instance ID (this can be used to track the status of the orchestration)
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
    }


}
