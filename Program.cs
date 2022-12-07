using Serilog;
using WorkerService_CheckURLS;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog()   // use serilog in the project
    .UseWindowsService()
    .ConfigureServices((hostContext,services) =>
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();// crear el log
        services.AddHostedService<Worker>();
        services.AddHttpClient(); // http dependecy for checking the url's
    })
    .Build();

await host.RunAsync();
