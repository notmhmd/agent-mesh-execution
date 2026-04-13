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

## Related repos

- `agent-mesh-contracts` — JSON schemas
- `agent-mesh-pipeline` — feed, signal, risk
- `agent-mesh-infra` — compose
