using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Options;

namespace Nienow.DataBaseConn
{
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

    /// <summary>Falha ao acessar o banco: a API responde 503 com a explicação.</summary>
    public sealed class DatabaseUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

    public sealed class ShinrikaiDB(IOptions<ShinrikaiDbOptions> options) : IShinrikaiDb
    {
        public const string Placeholder = "CONFIGURE_ME";

        private readonly string _connectionString = options.Value.ShinrikaiDB;

        public static bool IsConfigured(string? connectionString) =>
            !string.IsNullOrWhiteSpace(connectionString) &&
            !connectionString.Contains(Placeholder, StringComparison.OrdinalIgnoreCase);

        public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
        {
            if (!IsConfigured(_connectionString))
                throw new DatabaseUnavailableException(
                    "A conexão com o banco não está configurada (ConnectionStrings:ShinrikaiDB). " +
                    "Configure com: dotnet user-secrets --project Nienow.Shinrikai set \"ConnectionStrings:ShinrikaiDB\" \"...\"");

            var conn = new FbConnection(_connectionString);
            try
            {
                await conn.OpenAsync(ct);
                return conn;
            }
            catch (Exception ex) when (ex is FbException or NotSupportedException or System.Net.Sockets.SocketException)
            {
                await conn.DisposeAsync();
                throw new DatabaseUnavailableException(Explain(ex), ex);
            }
        }

        /// <summary>Traduz as falhas de conexão mais comuns para uma orientação.</summary>
        public static string Explain(Exception ex)
        {
            var message = ex.Message;
            if (message.Contains("unsupported on-disk structure", StringComparison.OrdinalIgnoreCase))
                return "O arquivo do banco está num formato antigo (Firebird 2.5, ODS 11) que o servidor Firebird 4 não abre. " +
                       "Converta com gbak (backup no 2.5, restore no 4) — ver README. Detalhe: " + message;
            if (message.Contains("Legacy_Auth", StringComparison.OrdinalIgnoreCase))
                return "O servidor Firebird recusou o usuário/senha pela autenticação SRP e tentou a antiga (Legacy_Auth), " +
                       "que o driver não suporta. Confira usuário e senha da connection string (o usuário precisa existir no SRP).";
            if (message.Contains("password", StringComparison.OrdinalIgnoreCase) || message.Contains("user name", StringComparison.OrdinalIgnoreCase))
                return "Usuário ou senha do banco inválidos. Detalhe: " + message;
            if (message.Contains("I/O error", StringComparison.OrdinalIgnoreCase) || message.Contains("CreateFile", StringComparison.OrdinalIgnoreCase))
                return "O servidor Firebird não encontrou/abriu o arquivo do banco (initial catalog). Detalhe: " + message;
            if (ex is System.Net.Sockets.SocketException || message.Contains("Unable to complete network request", StringComparison.OrdinalIgnoreCase))
                return "Não foi possível conectar ao servidor Firebird (data source/port). Detalhe: " + message;
            return "Não foi possível acessar o banco de dados. Detalhe: " + message;
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
}
