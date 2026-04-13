using Execution.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var redisConn =
    builder.Configuration["ConnectionStrings:Redis"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
    ?? Environment.GetEnvironmentVariable("REDIS_ADDR")
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var opts = ConfigurationOptions.Parse(redisConn);
    opts.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(opts);
});
builder.Services.AddHostedService<IntentConsumerWorker>();

var app = builder.Build();
await app.RunAsync();
