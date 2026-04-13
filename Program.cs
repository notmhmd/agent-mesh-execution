using Execution.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
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

var pgConn =
    builder.Configuration["ConnectionStrings:Postgres"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

var pgSource = string.IsNullOrWhiteSpace(pgConn)
    ? null
    : new NpgsqlDataSourceBuilder(pgConn).Build();

builder.Services.AddSingleton(new DataSources(pgSource));

builder.Services.AddHostedService<HeartbeatPublisher>();
builder.Services.AddHostedService<IntentConsumerWorker>();

var app = builder.Build();
await app.RunAsync();
