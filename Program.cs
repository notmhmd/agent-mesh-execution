using Execution.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var otlpEndpoint =
    Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
var otelDisabled = string.Equals(
    Environment.GetEnvironmentVariable("OTEL_SDK_DISABLED"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!otelDisabled && !string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(rb => rb.AddService(
            serviceName: "agent-mesh-execution",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0"))
        .WithTracing(tb => tb
            .AddSource(IntentConsumerWorker.ActivitySourceName)
            .AddOtlpExporter());
}

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

builder.Services.AddHostedService<PrometheusMetricServer>();
builder.Services.AddHostedService<HeartbeatPublisher>();
builder.Services.AddHostedService<IntentConsumerWorker>();

var app = builder.Build();
await app.RunAsync();
