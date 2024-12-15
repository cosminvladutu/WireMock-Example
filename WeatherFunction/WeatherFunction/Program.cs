using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services. // Register HttpClients for OpenCage and Open-Meteo
        AddHttpClient("OpenCage", client =>
        {
            client.BaseAddress = new Uri("https://geocode.maps.co/");
        });
builder.Services.AddHttpClient("OpenMeteo", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/v1/");
});

builder.Build().Run();
