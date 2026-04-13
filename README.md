# agent-mesh-execution

**.NET 9** execution gateway: **Redis Streams** consumer group `execution` on `stream:approved:intents` (field `data` = JSON). Inserts into Postgres `execution_events`, **XACK** after commit. Alpaca submit next.

Patterns: `NpgsqlDataSource` pooling, source-generated `System.Text.Json`, `PeriodicTimer` heartbeat (see `HeartbeatPublisher`).

## Env

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__Redis` | e.g. `redis:6379,abortConnect=false` |
| `ConnectionStrings__Postgres` | Npgsql connection string (optional; skips DB if unset) |
| `REDIS_ADDR` | Legacy fallback for Redis |
| `APCA_API_KEY_ID` | Alpaca (wire SDK next) |
| `APCA_API_SECRET_KEY` | Alpaca secret |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | e.g. `http://otel-collector:4317` — enables OTLP traces (`Execution.Gateway` source) |
| `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` | Alternative to combined OTLP endpoint |
| `OTEL_SDK_DISABLED` | Set `true` to disable tracing even if endpoint is set |

## Build

```bash
dotnet build
dotnet run
```

## Metrics

Prometheus **`/metrics`** on port **9090** (`METRICS_PORT`). Counters: `agentmesh_execution_intents_consumed_total`, `agentmesh_execution_intent_errors_total`; gauge: `agentmesh_execution_heartbeat_unixtime`.

## Docker

```bash
docker build -t agent-mesh-execution .
```

## ApprovedIntent extras

Optional **`traceparent`** (W3C) may be set by **signal-agent** when OTLP is enabled so this service can link **`ProcessStreamEntry`** as a child span.

## Related repos

- `agent-mesh-contracts` — JSON schemas
- `agent-mesh-pipeline` — feed, signal, risk
- `agent-mesh-infra` — compose
