// Execution gateway: consume approved intents from Redis, submit to Alpaca (wire next), persist results.
using StackExchange.Redis;

var redisConn = Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
    ?? Environment.GetEnvironmentVariable("REDIS_ADDR")
    ?? "localhost:6379";

Console.WriteLine($"Execution.Gateway | redis={redisConn}");

try
{
    var opts = ConfigurationOptions.Parse(redisConn);
    opts.AbortOnConnectFail = false;
    var mux = await ConnectionMultiplexer.ConnectAsync(opts);
    var db = mux.GetDatabase();
    await db.PingAsync();
    Console.WriteLine("Redis OK");
    mux.Dispose();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Redis: {ex.Message}");
}

// TODO: IHostedService — BRPOP approved:intents, Alpaca SDK submit, idempotent writes to Postgres
await Task.Delay(Timeout.Infinite);
