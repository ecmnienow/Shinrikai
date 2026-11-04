using System.Data;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Options;

namespace Nienow.DataBaseConn;

public sealed class ShinrikaiDbOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>Connection string do Firebird (antes em Web.config &gt; connectionStrings &gt; ShinrikaiDB).</summary>
    public string ShinrikaiDB { get; set; } = string.Empty;
}

/// <summary>
/// Fábrica de conexões. Substitui o antigo ShinrikaiDB, que guardava um DbCommand compartilhado
/// por instância (e por sessão MVC). Aqui cada operação abre sua própria conexão (o pool do driver
/// cuida do custo) e todo valor vai por parâmetro — ver <see cref="DbExtensions"/>.
/// </summary>
public interface IShinrikaiDb
{
    Task<DbConnection> OpenAsync(CancellationToken ct = default);
}

public sealed class ShinrikaiDB(IOptions<ShinrikaiDbOptions> options) : IShinrikaiDb
{
    private readonly string _connectionString = options.Value.ShinrikaiDB;

    public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings:ShinrikaiDB não configurada.");

        var conn = new FbConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}

/// <summary>Helpers finos sobre ADO.NET. Não há sobrecarga que aceite SQL + valores concatenados.</summary>
public static class DbExtensions
{
    public static DbCommand Command(this DbConnection conn, string sql, DbTransaction? tx = null, params (string Name, object? Value)[] parameters)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var (name, value) in parameters)
            cmd.AddParameter(name, value);
        return cmd;
    }

    public static DbParameter AddParameter(this DbCommand cmd, string name, object? value, DbType? type = null)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        if (type.HasValue)
            p.DbType = type.Value;
        cmd.Parameters.Add(p);
        return p;
    }

    public static async Task<List<T>> QueryAsync<T>(this DbConnection conn, string sql, Func<DbDataReader, T> map, CancellationToken ct = default, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = conn.Command(sql, null, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await reader.ReadAsync(ct))
            list.Add(map(reader));
        return list;
    }

    public static async Task<T?> QuerySingleOrDefaultAsync<T>(this DbConnection conn, string sql, Func<DbDataReader, T> map, CancellationToken ct = default, params (string Name, object? Value)[] parameters)
        where T : class
    {
        await using var cmd = conn.Command(sql, null, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? map(reader) : null;
    }

    public static async Task<object?> ScalarAsync(this DbConnection conn, string sql, CancellationToken ct = default, params (string Name, object? Value)[] parameters)
    {
        await using var cmd = conn.Command(sql, null, parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull ? null : result;
    }

    /// <summary>Executa em transação própria, com commit/rollback (padrão de todos os Add*/Update* do original).</summary>
    public static async Task<int> ExecuteInTransactionAsync(this DbConnection conn, string sql, CancellationToken ct = default, params (string Name, object? Value)[] parameters)
    {
        await using var tx = await conn.BeginTransactionAsync(ct);
        await using var cmd = conn.Command(sql, tx, parameters);
        try
        {
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            return rows;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // Leitura tolerante a NULL, como os "as int?" / "as string" do original.
    public static int GetInt(this DbDataReader r, string column) =>
        r[column] is DBNull or null ? 0 : Convert.ToInt32(r[column]);

    public static int? GetNullableInt(this DbDataReader r, string column) =>
        r[column] is DBNull or null ? null : Convert.ToInt32(r[column]);

    public static string? GetNullableString(this DbDataReader r, string column) =>
        r[column] is DBNull or null ? null : Convert.ToString(r[column])?.TrimEnd();

    public static DateTime GetDateTimeOrDefault(this DbDataReader r, string column) =>
        r[column] is DateTime dt ? dt : default;

    public static byte[]? GetBytes(this DbDataReader r, string column) =>
        r[column] as byte[];

    /// <summary>Gera ":p0, :p1, ..." e os parâmetros correspondentes para cláusulas IN.</summary>
    public static (string Sql, (string Name, object? Value)[] Parameters) InClause<T>(IReadOnlyList<T> values, string prefix = "p")
    {
        var names = values.Select((_, i) => $"@{prefix}{i}").ToArray();
        var parameters = values.Select((v, i) => (names[i], (object?)v)).ToArray();
        return (string.Join(", ", names), parameters);
    }
}
