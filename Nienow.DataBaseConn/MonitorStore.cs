using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.DataBaseConn
{
    /// <summary>
    /// Descreve uma tabela de monitor do original (VOC_MONITOR, TRL_MONITOR, KAN_MONITOR, EXE_MONITOR).
    /// Todas seguem o mesmo layout: {P}ID, {P}USR_ID, {item}, {P}COUNTER, {P}FAILS, {P}SUCCESSES, {P}LASTHIT.
    /// Os nomes vêm só daqui (conjunto fechado), nunca de entrada do usuário.
    /// </summary>
    public sealed record MonitorTable(string Table, string Prefix, string ItemColumn, string Generator, bool HasCounter = true)
    {
        public string Id => Prefix + "ID";
        public string User => Prefix + "USR_ID";
        public string Counter => Prefix + "COUNTER";
        public string Fails => Prefix + "FAILS";
        public string Successes => Prefix + "SUCCESSES";
        public string LastHit => Prefix + "LASTHIT";
    }

    public static class MonitorTables
    {
        /// <summary>Estudo de vocabulário (DBVocabulary).</summary>
        public static readonly MonitorTable Vocabulary = new("VOC_MONITOR", "VMN_", "VMN_VCB_ID", "VOCMONITOR_ID_GEN");

        /// <summary>
        /// Transcrição (DBTranscription) e palavras do stretchtext (DBText) — a mesma tabela.
        /// O original inseria pelo DBText usando o gerador VOCMONITOR_ID_GEN (de outra tabela), o que
        /// podia gerar ID repetido; aqui ambos usam o gerador da própria tabela.
        /// </summary>
        public static readonly MonitorTable Transcription = new("TRL_MONITOR", "TMN_", "TMN_VCB_ID", "TRLCMONITOR_ID_GEN");

        /// <summary>Leitura de kanjis (DBKanji).</summary>
        public static readonly MonitorTable Kanji = new("KAN_MONITOR", "KMN_", "KMN_KNJ_ID", "KANMONITOR_ID_GEN");

        /// <summary>Exercícios (DBExercise) — sem coluna de contador.</summary>
        public static readonly MonitorTable Exercise = new("EXE_MONITOR", "EMN_", "EMN_EXR_ID", "EXEMONITOR_ID_GEN", HasCounter: false);
    }

    public interface IMonitorStore
    {
        Task<Monitor?> GetAsync(MonitorTable table, int userId, int itemId, CancellationToken ct = default);

        /// <summary>Contador de cada item para o usuário (só itens com registro).</summary>
        Task<IReadOnlyDictionary<int, int>> GetCountersAsync(MonitorTable table, int userId, IReadOnlyList<int> itemIds, CancellationToken ct = default);

        Task SaveAsync(MonitorTable table, Monitor monitor, CancellationToken ct = default);
    }

    public sealed class MonitorStore(IShinrikaiDb db) : IMonitorStore
    {
        public async Task<Monitor?> GetAsync(MonitorTable t, int userId, int itemId, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            return await conn.QuerySingleOrDefaultAsync(
                $"SELECT * FROM {t.Table} WHERE {t.ItemColumn} = @ITEM AND {t.User} = @USR",
                r => new Monitor
                {
                    ID = r.GetInt(t.Id),
                    UsrID = r.GetInt(t.User),
                    VcbID = r.GetInt(t.ItemColumn),
                    Counter = t.HasCounter ? r.GetInt(t.Counter) : 0,
                    FailCounter = r.GetInt(t.Fails),
                    SuccessCounter = r.GetInt(t.Successes),
                    LastHittedTime = r.GetDateTimeOrDefault(t.LastHit),
                },
                ct, ("@ITEM", itemId), ("@USR", userId));
        }

        public async Task<IReadOnlyDictionary<int, int>> GetCountersAsync(MonitorTable t, int userId, IReadOnlyList<int> itemIds, CancellationToken ct = default)
        {
            var result = new Dictionary<int, int>();
            if (itemIds.Count == 0 || !t.HasCounter)
                return result;

            var (inSql, parameters) = DbExtensions.InClause(itemIds.Distinct().ToList());
            await using var conn = await db.OpenAsync(ct);
            var rows = await conn.QueryAsync(
                $"SELECT {t.ItemColumn}, {t.Counter} FROM {t.Table} WHERE {t.User} = @USR AND {t.ItemColumn} IN ({inSql})",
                r => (Item: r.GetInt(t.ItemColumn), Counter: r.GetInt(t.Counter)),
                ct, [("@USR", (object?)userId), .. parameters]);

            foreach (var (item, counter) in rows)
                result.TryAdd(item, counter);

            return result;
        }

        public async Task SaveAsync(MonitorTable t, Monitor m, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            if (m.IsNew)
            {
                var counterColumn = t.HasCounter ? $"{t.Counter}, " : "";
                var counterValue = t.HasCounter ? "@CNT, " : "";
                await conn.ExecuteInTransactionAsync(
                    $"""
                    INSERT INTO {t.Table} (
                      {t.Id}, {t.User}, {t.ItemColumn}, {counterColumn}{t.Fails}, {t.Successes}, {t.LastHit}
                    ) VALUES (
                      GEN_ID({t.Generator}, 1), @USR, @ITEM, {counterValue}@FAI, @SUC, @HIT
                    )
                    """,
                    ct, Parameters(m, t.HasCounter, includeIds: true));
            }
            else
            {
                var counterSet = t.HasCounter ? $"{t.Counter} = @CNT, " : "";
                await conn.ExecuteInTransactionAsync(
                    $"UPDATE {t.Table} SET {counterSet}{t.Fails} = @FAI, {t.Successes} = @SUC, {t.LastHit} = @HIT WHERE {t.Id} = @ID",
                    ct, Parameters(m, t.HasCounter, includeIds: false));
            }
        }

        private static (string, object?)[] Parameters(Monitor m, bool hasCounter, bool includeIds)
        {
            var list = new List<(string, object?)>
            {
                ("@FAI", m.FailCounter),
                ("@SUC", m.SuccessCounter),
                ("@HIT", m.LastHittedTime),
            };
            if (hasCounter)
                list.Add(("@CNT", m.Counter));
            if (includeIds)
            {
                list.Add(("@USR", m.UsrID));
                list.Add(("@ITEM", m.VcbID));
            }
            else
            {
                list.Add(("@ID", m.ID));
            }
            return list.ToArray();
        }
    }

    /// <summary>
    /// Tabelas de "conhecimento declarado" (GRA_MONITOR, TEX_MONITOR): um registro por usuário e item,
    /// criado com {P}KNOWLEDGE = 'S' e com {P}ACCESS atualizado a cada nova declaração.
    /// </summary>
    public sealed record KnowledgeTable(string Table, string Prefix, string ItemColumn, string Generator)
    {
        public string Id => Prefix + "ID";
        public string User => Prefix + "USR_ID";
        public string Knowledge => Prefix + "KNOWLEDGE";
        public string Access => Prefix + "ACCESS";
    }

    public static class KnowledgeTables
    {
        public static readonly KnowledgeTable Grammar = new("GRA_MONITOR", "GMN_", "GMN_GRM_ID", "GRAMONITOR_ID_GEN");
        public static readonly KnowledgeTable Text = new("TEX_MONITOR", "TMN_", "TMN_TXT_ID", "TEXMONITOR_ID_GEN");
    }

    public interface IKnowledgeStore
    {
        /// <summary>DeclareKnowledge do original (DBGrammar/DBText).</summary>
        Task DeclareAsync(KnowledgeTable table, int userId, int itemId, DateTime now, CancellationToken ct = default);

        /// <summary>IDs de itens que o usuário já declarou.</summary>
        Task<IReadOnlySet<int>> GetDeclaredAsync(KnowledgeTable table, int userId, CancellationToken ct = default);
    }

    public sealed class KnowledgeStore(IShinrikaiDb db) : IKnowledgeStore
    {
        public async Task DeclareAsync(KnowledgeTable t, int userId, int itemId, DateTime now, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            var existing = await conn.ScalarAsync(
                $"SELECT {t.Id} FROM {t.Table} WHERE {t.ItemColumn} = @ITEM AND {t.User} = @USR",
                ct, ("@ITEM", itemId), ("@USR", userId));

            if (existing is not null)
            {
                await conn.ExecuteInTransactionAsync(
                    $"UPDATE {t.Table} SET {t.Access} = @ACC WHERE {t.Id} = @ID",
                    ct, ("@ACC", now), ("@ID", Convert.ToInt32(existing)));
            }
            else
            {
                await conn.ExecuteInTransactionAsync(
                    $"""
                    INSERT INTO {t.Table} ({t.Id}, {t.User}, {t.ItemColumn}, {t.Knowledge}, {t.Access})
                    VALUES (GEN_ID({t.Generator}, 1), @USR, @ITEM, 'S', @ACC)
                    """,
                    ct, ("@USR", userId), ("@ITEM", itemId), ("@ACC", now));
            }
        }

        public async Task<IReadOnlySet<int>> GetDeclaredAsync(KnowledgeTable t, int userId, CancellationToken ct = default)
        {
            await using var conn = await db.OpenAsync(ct);
            var ids = await conn.QueryAsync(
                $"SELECT {t.ItemColumn} FROM {t.Table} WHERE {t.User} = @USR",
                r => r.GetInt(t.ItemColumn), ct, ("@USR", userId));
            return ids.ToHashSet();
        }
    }
}
