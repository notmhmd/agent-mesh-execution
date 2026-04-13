using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;

namespace Execution.Gateway;

/// <summary>
/// Redis Streams consumer group (at-least-once); XACK after durable log to Postgres.
/// </summary>
public sealed class IntentConsumerWorker : BackgroundService
{
    public const string StreamKey = "stream:approved:intents";
    public const string GroupName = "execution";
    private readonly IConnectionMultiplexer _mux;
    private readonly DataSources _data;
    private readonly ILogger<IntentConsumerWorker> _log;
    private readonly string _consumerName =
        $"{Environment.MachineName}-{Environment.ProcessId}";

    public IntentConsumerWorker(
        IConnectionMultiplexer mux,
        DataSources data,
        ILogger<IntentConsumerWorker> log)
    {
        _mux = mux;
        _data = data;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _mux.GetDatabase();
        await EnsureConsumerGroupAsync(db).ConfigureAwait(false);

        _log.LogInformation(
            "Stream consumer {Consumer} on {Stream} group {Group}",
            _consumerName,
            StreamKey,
            GroupName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    StreamKey,
                    GroupName,
                    _consumerName,
                    ">",
                    count: 32,
                    noAck: false).ConfigureAwait(false);

                if (entries.Length == 0)
                {
                    await Task.Delay(50, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var entry in entries)
                {
                    await ProcessEntryAsync(db, entry, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "stream read loop");
                await Task.Delay(500, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db)
    {
        try
        {
            // "0" = deliver stream backlog after group is created (avoids race if publisher XADDs first)
            await db.StreamCreateConsumerGroupAsync(
                StreamKey,
                GroupName,
                "0",
                true).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.Ordinal))
        {
            // already exists
        }
    }

    private async Task ProcessEntryAsync(
        IDatabase db,
        StreamEntry entry,
        CancellationToken ct)
    {
        string? raw = null;
        foreach (var nv in entry.Values)
        {
            if (nv.Name.ToString() != "data")
                continue;
            raw = nv.Value.ToString();
            break;
        }

        if (string.IsNullOrEmpty(raw))
        {
            _log.LogWarning("Stream entry {Id} missing data field", entry.Id);
            await db.StreamAcknowledgeAsync(StreamKey, GroupName, entry.Id).ConfigureAwait(false);
            return;
        }

        ApprovedIntentV1? intent = null;
        try
        {
            intent = JsonSerializer.Deserialize(raw, ExecutionJsonContext.Default.ApprovedIntentV1);
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "invalid JSON in stream entry {Id}", entry.Id);
        }

        _log.LogInformation("Consumed intent {IntentId} id={EntryId}", intent?.IntentId, entry.Id);

        if (_data.Postgres is { } pg)
        {
            await using var conn = await pg.OpenConnectionAsync(ct).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO execution_events (intent_id, trace_id, status, detail) VALUES ($1, $2, 'CONSUMED', $3::jsonb)",
                conn);
            cmd.Parameters.AddWithValue((object?)intent?.IntentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue((object?)intent?.TraceId ?? DBNull.Value);
            cmd.Parameters.Add(
                new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = raw });
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await db.StreamAcknowledgeAsync(StreamKey, GroupName, entry.Id).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        await db.StringSetAsync(
            "execution:heartbeat",
            now,
            TimeSpan.FromMinutes(5)).ConfigureAwait(false);
    }
}
