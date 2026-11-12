using System.Diagnostics.CodeAnalysis;
using Npgsql;
using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// PostgreSQL-backed cost tracker for reasoning chain token usage.
/// Persists usage data to a chain_usage table for cross-restart durability.
/// </summary>
[RequiresUnreferencedCode("Calls Npgsql.NpgsqlCommand.ExecuteReader()")]
internal sealed class PostgresCostTracker(IConfiguration configuration, ILogger<PostgresCostTracker> logger)
    : ICostTracker
{
    private readonly string _connectionString = configuration.GetConnectionString("PostgreSQL")
                                                ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");

    private bool _tableEnsured;

    private const string TableName = "chain_usage";

    public void TrackUsage(Guid chainId, int inputTokenCount, int outputTokenCount, string modelId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // Ensure table exists on first use
            if (!_tableEnsured)
            {
                EnsureTable(connection);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO {TableName} (chain_id, input_tokens, output_tokens, model_id, recorded_at)
                VALUES (@chainId, @input, @output, @model, @now)
                ON CONFLICT (chain_id) DO UPDATE SET
                    input_tokens = @input,
                    output_tokens = @output,
                    model_id = @model,
                    recorded_at = @now
                """;

            cmd.Parameters.AddWithValue("@chainId", chainId);
            cmd.Parameters.AddWithValue("@input", inputTokenCount);
            cmd.Parameters.AddWithValue("@output", outputTokenCount);
            cmd.Parameters.AddWithValue("@model", modelId);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist cost usage for chain {ChainId}", chainId);
        }
    }

    public CostUsageSnapshot? GetUsage(Guid chainId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                SELECT chain_id, input_tokens, output_tokens, model_id, recorded_at
                FROM {TableName}
                WHERE chain_id = @chainId
                """;
            cmd.Parameters.AddWithValue("@chainId", chainId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new CostUsageSnapshot(
                    reader.GetGuid(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetDateTime(4));
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read cost usage for chain {ChainId}", chainId);
            return null;
        }
    }

    public async Task<IReadOnlyList<CostUsageSnapshot>> GetRecentUsageAsync(
        string modelId, int limit = 50, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT chain_id, input_tokens, output_tokens, model_id, recorded_at
            FROM {TableName}
            WHERE model_id = @model
            ORDER BY recorded_at DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@model", modelId);
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<CostUsageSnapshot>();

        while (await reader.ReadAsync(ct))
        {
            results.Add(new CostUsageSnapshot(
                reader.GetGuid(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetDateTime(4)));
        }

        return results;
    }

    public async Task<CostAggregate> GetAggregateAsync(
        string modelId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT
                COALESCE(SUM(input_tokens), 0),
                COALESCE(SUM(output_tokens), 0),
                COUNT(*)
            FROM {TableName}
            WHERE model_id = @model
              AND recorded_at >= @from
              AND recorded_at < @to
            """;
        cmd.Parameters.AddWithValue("@model", modelId);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new CostAggregate(
                modelId,
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                from,
                to);
        }

        return new CostAggregate(modelId, 0, 0, 0, from, to);
    }

    public async Task EnsureTableExistsAsync(CancellationToken ct = default)
    {
        if (_tableEnsured) return;

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            await EnsureTableAsync(connection, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create {TableName} table", TableName);
        }
    }

    private void EnsureTable(NpgsqlConnection connection)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS {TableName} (
                    chain_id UUID PRIMARY KEY,
                    input_tokens INTEGER NOT NULL DEFAULT 0,
                    output_tokens INTEGER NOT NULL DEFAULT 0,
                    model_id VARCHAR(100) NOT NULL DEFAULT 'unknown',
                    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_{TableName}_model_id
                    ON {TableName} (model_id);

                CREATE INDEX IF NOT EXISTS idx_{TableName}_recorded_at
                    ON {TableName} (recorded_at);
                """;

            cmd.ExecuteNonQuery();
            _tableEnsured = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create {TableName} table synchronously", TableName);
        }
    }

    private async Task EnsureTableAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                chain_id UUID PRIMARY KEY,
                input_tokens INTEGER NOT NULL DEFAULT 0,
                output_tokens INTEGER NOT NULL DEFAULT 0,
                model_id VARCHAR(100) NOT NULL DEFAULT 'unknown',
                recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_{TableName}_model_id
                ON {TableName} (model_id);

            CREATE INDEX IF NOT EXISTS idx_{TableName}_recorded_at
                ON {TableName} (recorded_at);
            """;

        await cmd.ExecuteNonQueryAsync(ct);
        _tableEnsured = true;
    }
}
