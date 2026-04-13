# agent-mesh-execution

**Go** service: consumes `ApprovedIntent` messages from Redis, submits to Alpaca (to be wired), persists `ExecutionResult`. No LLM on this path.

## Env

| Variable | Default | Description |
|----------|---------|-------------|
| `REDIS_ADDR` | `localhost:6379` | Redis address |
| `APCA_API_KEY_ID` | — | Alpaca key |
| `APCA_API_SECRET_KEY` | — | Alpaca secret |
| `APCA_API_BASE_URL` | paper URL | Broker base URL |

## Build

```bash
go build -o gateway ./cmd/gateway
```

## Docker

```bash
docker build -t agent-mesh-execution .
```

## Related repos

- `agent-mesh-contracts` — JSON schemas
- `agent-mesh-pipeline` — feed + signal + risk producers
- `agent-mesh-infra` — compose stack
