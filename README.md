# agent-mesh-execution

**.NET 8** execution gateway: consumes `ApprovedIntent` messages from Redis, will submit to Alpaca with idempotency and persist results. Keeps LLM/agent logic off the hot path.

## Why .NET here

Strong fit for long-running services, async I/O (Redis, HTTP, Postgres), typed domain models for orders/intents, and straightforward observability (OpenTelemetry) as the service grows.

## Env

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__Redis` | e.g. `redis:6379,abortConnect=false` |
| `REDIS_ADDR` | Fallback if `ConnectionStrings__Redis` unset |
| `APCA_API_KEY_ID` | Alpaca (wire Alpaca SDK next) |
| `APCA_API_SECRET_KEY` | Alpaca secret |

## Build

```bash
dotnet build
dotnet run
```

## Docker

```bash
docker build -t agent-mesh-execution .
```

## Related repos

- `agent-mesh-contracts` — JSON schemas
- `agent-mesh-pipeline` — feed, signal, risk
- `agent-mesh-infra` — compose
