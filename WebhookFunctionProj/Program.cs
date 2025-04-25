// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.DependencyInjection;

// var host = new HostBuilder()
//     .ConfigureFunctionsWebApplication()
//     .ConfigureServices(services => {
//         services.AddApplicationInsightsTelemetryWorkerService();
//         services.ConfigureFunctionsApplicationInsights();
//     })
//     .Build();

// host.Run();

using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
