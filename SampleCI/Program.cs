using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleCI;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IServiceBusFactory>(new ServiceBusFactory());
    })
    .Build();

host.Run();
