using Npgsql;

namespace Execution.Gateway;

/// <summary>Holds optional Postgres pool (DI-friendly; avoids nullable generic constraints).</summary>
public sealed class DataSources
{
    public DataSources(NpgsqlDataSource? postgres) => Postgres = postgres;

    public NpgsqlDataSource? Postgres { get; }
}
