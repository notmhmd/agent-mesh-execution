using Prometheus;

namespace Execution.Gateway;

/// <summary>Prometheus metrics (scraped from <c>/metrics</c> on <see cref="PrometheusMetricServer"/> port).</summary>
public static class AgentMetrics
{
    public static readonly Counter IntentsConsumed = Metrics.CreateCounter(
        "agentmesh_execution_intents_consumed_total",
        "Approved intents consumed from Redis Streams and ACKed");

    public static readonly Counter IntentProcessingErrors = Metrics.CreateCounter(
        "agentmesh_execution_intent_errors_total",
        "Failures while processing a stream entry");

    public static readonly Gauge HeartbeatUnix = Metrics.CreateGauge(
        "agentmesh_execution_heartbeat_unixtime",
        "Unix timestamp written to execution:heartbeat");
}
