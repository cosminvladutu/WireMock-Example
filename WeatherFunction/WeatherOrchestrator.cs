using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

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



//using Microsoft.Azure.Functions.Worker;
//using Microsoft.DurableTask;

//[DurableTask]
//public class WeatherOrchestrator
//{
//    [Function(nameof(WeatherOrchestrator))]
//    public async Task<string> RunAsync(
//               [OrchestrationTrigger] TaskOrchestrationContext context)
//    {
//        var city = context.GetInput<string>();


//        var coordinates = await context.CallActivityAsync<(double lat, double lon)>(nameof(GetCoordinates), city);
//        var weatherData = await context.CallActivityAsync<string>(nameof(GetWeatherData), coordinates);

//        return await Task.FromResult(city);
//    }
//}
