using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Execution.Gateway;

/// <summary>BRPOP from approved:intents; updates execution:heartbeat. Alpaca submit wired later.</summary>
public sealed class IntentConsumerWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<IntentConsumerWorker> _log;

    public IntentConsumerWorker(IConnectionMultiplexer mux, ILogger<IntentConsumerWorker> log)
    {
        _mux = mux;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _mux.GetDatabase();
        _log.LogInformation("IntentConsumer listening on approved:intents (BRPOP)");

        var hb = Task.Run(
            async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        await db.StringSetAsync(
                            "execution:heartbeat",
                            now,
                            TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _log.LogDebug(ex, "heartbeat tick");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken).ConfigureAwait(false);
                }
            },
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await db.ExecuteAsync("BRPOP", "approved:intents", "5").ConfigureAwait(false);
                if (result.IsNull)
                    continue;

                // BRPOP returns [key, value]
                var arr = (RedisValue[])result!;
                if (arr.Length < 2)
                    continue;

                var payload = (string)arr[1]!;
                _log.LogInformation("Dequeued intent: {Payload}", payload);

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                await db.StringSetAsync(
                    "execution:heartbeat",
                    now,
                    TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "BRPOP loop error");
                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            }
        }

        await hb.ConfigureAwait(false);
    }
}
