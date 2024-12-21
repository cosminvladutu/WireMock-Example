using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace WeatherFunction.Triggers
{
    public class Function2
    {
        private readonly ILogger<Function2> _logger;

        public Function2(ILogger<Function2> logger)
        {
            _logger = logger;
        }

        [Function("Function2")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weather2/{city}")] HttpRequest req,
            [DurableClient] DurableTaskClient starter,
            string city)
        {

            var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(WeatherOrchestrator), city);
            //var response = req.CreateResponse(HttpStatusCode.Accepted);
            //return response;

            //_logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
