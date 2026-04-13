using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Execution.Gateway;

/// <summary>Exposes Prometheus scrape endpoint (no ASP.NET — lightweight TCP server).</summary>
public sealed class PrometheusMetricServer : IHostedService, IDisposable
{
    private readonly ILogger<PrometheusMetricServer> _log;
    private readonly int _port;
    private MetricServer? _server;

    public PrometheusMetricServer(ILogger<PrometheusMetricServer> log)
    {
        _log = log;
        var raw = Environment.GetEnvironmentVariable("METRICS_PORT");
        _port = int.TryParse(raw, out var p) ? p : 9090;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server = new MetricServer(_port);
        _server.Start();
        _log.LogInformation("Prometheus /metrics on port {Port}", _port);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() => _server?.Stop();
}
