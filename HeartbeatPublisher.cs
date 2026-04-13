using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Execution.Gateway;

/// <summary>Periodic heartbeat for dashboards / guardians (no busy-wait).</summary>
public sealed class HeartbeatPublisher : BackgroundService
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<HeartbeatPublisher> _log;

    public HeartbeatPublisher(IConnectionMultiplexer mux, ILogger<HeartbeatPublisher> log)
    {
        _mux = mux;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _mux.GetDatabase();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(25));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    AgentMetrics.HeartbeatUnix.Set(unix);
                    var now = unix.ToString();
                    await db.StringSetAsync(
                        "execution:heartbeat",
                        now,
                        TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogDebug(ex, "heartbeat tick");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }
}
