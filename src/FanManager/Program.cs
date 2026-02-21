using FanManager.Core.Interfaces;
using FanManager.Core.Options;
using FanManager.Core.Services;
using FanManager.Infrastructure.Services;
using FanManager.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Only Read environment variables prefixed
builder.Configuration.AddEnvironmentVariables(prefix: "FANMANAGER__");

builder.Services
    .AddOptions<FanManagerOptions>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services
    .AddOptions<IpmiOptions>()
    .Bind(builder.Configuration.GetSection("IPMI"))
    .ValidateOnStart();

builder.Services.AddOptions<ProcessServiceOptions>()
    .Bind(builder.Configuration.GetSection("PROCESS"))
    .ValidateOnStart();

// Configure logging
builder.Logging.AddConsole();

// Register Core services
builder.Services.AddSingleton<FanControlStrategy>();

// Register Infrastructure services
builder.Services.AddSingleton<IIpmiService, IpmiService>();
builder.Services.AddSingleton<INvidiaSmiService, NvidiaSmiService>();
builder.Services.AddSingleton<IProcessService, ProcessService>();

// Register Worker
builder.Services.AddHostedService<FanManagerWorker>();

var host = builder.Build();
await host.RunAsync();
