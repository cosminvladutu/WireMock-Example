using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.DurableTask.Client;
//using Microsoft.Azure.Functions.Extensions.DurableTask;
//using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace WeatherFunction.Triggers
{

    // HTTP trigger function to start the orchestration
    public class GetWeatherHttpTrigger
    {
        //private readonly IDurableTaskClient _durableClient;

        //public GetWeatherHttpTrigger(IDurableTaskClient durableClient)
        //{
        //    _durableClient = durableClient;
        //}

        [Function("GetWeather")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/{city}")] HttpRequestData req,
            string city,
            //FunctionContext context)
            [DurableClient] DurableTaskClient starter)
        {
            //var logger = context.GetLogger("GetWeather");
            //logger.LogInformation($"Starting weather workflow for city: {city}");

            // Start the orchestrator
            var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WeatherOrchestrator), city);

            // Return the orchestration instance ID (this can be used to track the status of the orchestration)
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            //response.Headers.Add("Location", starter.CreateCheckStatusReturnUrl(req, instanceId));
            return response;
        }
    }


}
