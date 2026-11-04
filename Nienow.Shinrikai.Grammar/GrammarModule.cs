using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;

namespace Nienow.Shinrikai.Grammar
{
    /// <summary>Port de TGrammar.cs (o original chamava o ID de "Index").</summary>
    public class TGrammar
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public EJLPT Level { get; set; } = EJLPT.None;
        public string Body { get; set; } = string.Empty;
        public int[] Requisites { get; set; } = [];

        /// <summary>Valor cru de GRM_REQUISITES (para a regra de HasKnowledge do original).</summary>
        [JsonIgnore]
        public string? RawRequisites { get; set; }
    }

    /// <summary>Item do mapa (GGrammar).</summary>
    public record GrammarMapItem(int Id, string Title, EJLPT Level, bool HasEnoughLevel, bool HasExercises);

    public record GrammarMapGroup(EJLPT Level, IReadOnlyList<GrammarMapItem> Items);

    /// <summary>Resposta de GetRequisites: por que o conteúdo está bloqueado.</summary>
    public record GrammarRequisites(bool LevelTooHigh, string Message, IReadOnlyList<string> Requisites);

    public record GrammarSummary(int Id, string Title);

    public sealed class GrammarForm
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        /// <summary>"none", "N5"…"N1" (case-insensitive).</summary>
        public string? Level { get; set; }
        public string? Body { get; set; }
        public int[]? Requisites { get; set; }
    }

    public interface IGrammarRepository
    {
        /// <summary>Todos os conteúdos, na ordem de DBGrammar.ListGrammars (sem requisitos primeiro).</summary>
        Task<IReadOnlyList<TGrammar>> ListAsync(CancellationToken ct = default);
        /// <summary>DBGrammar.ListGrammars(jlpt) — usado no cadastro para escolher pré-requisitos.</summary>
        Task<IReadOnlyList<GrammarSummary>> ListByLevelAsync(EJLPT? level, CancellationToken ct = default);
        Task<TGrammar?> GetAsync(int id, CancellationToken ct = default);
        Task<IReadOnlySet<int>> GetGrammarsWithExercisesAsync(CancellationToken ct = default);
        Task AddAsync(string title, string? level, string body, int[] requisites, CancellationToken ct = default);
        Task UpdateAsync(int id, string title, string? level, string body, int[] requisites, CancellationToken ct = default);
    }

    /// <summary>Port de DBGrammar.cs (SQL parametrizado).</summary>
    public sealed class GrammarRepository(IShinrikaiDb db) : IGrammarRepository
    {
        private const string OrderBy = "ORDER BY (CASE WHEN GRM_REQUISITES IS NULL THEN 0 ELSE 1 END), GRM_REQUISITES, 1";

        public Task<IReadOnlyList<TGrammar>> ListAsync(CancellationToken ct = default) =>
            db.QueryAsync($"SELECT * FROM GRAMMAR {OrderBy}", Map, ct);

        public Task<IReadOnlyList<GrammarSummary>> ListByLevelAsync(EJLPT? level, CancellationToken ct = default)
        {
            var (where, parameters) = level switch
            {
                null => ("", Array.Empty<(string, object?)>()),
                EJLPT.None or EJLPT.N5 => ("WHERE GRM_LEVEL IS NULL OR UPPER(GRM_LEVEL) = 'N5'", []),
                _ => ("WHERE UPPER(GRM_LEVEL) = @LVL", [("@LVL", level.Value.ToString().ToUpperInvariant())]),
            };
            return db.QueryAsync(
                $"SELECT GRM_ID, GRM_TITLE FROM GRAMMAR {where} {OrderBy}",
                r => new GrammarSummary(r.GetInt("GRM_ID"), r.GetNullableString("GRM_TITLE") ?? ""), ct, parameters);
        }

        public Task<TGrammar?> GetAsync(int id, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync("SELECT * FROM GRAMMAR WHERE GRM_ID = @ID", Map, ct, ("@ID", id));

        public async Task<IReadOnlySet<int>> GetGrammarsWithExercisesAsync(CancellationToken ct = default) =>
            (await db.QueryAsync("SELECT DISTINCT EXR_GRM_ID FROM EXERCISES WHERE EXR_GRM_ID IS NOT NULL", r => r.GetInt("EXR_GRM_ID"), ct)).ToHashSet();

        public Task AddAsync(string title, string? level, string body, int[] requisites, CancellationToken ct = default) =>
            db.ExecuteAsync(
                """
                INSERT INTO GRAMMAR (GRM_ID, GRM_TITLE, GRM_LEVEL, GRM_BODY, GRM_REQUISITES)
                VALUES (GEN_ID(GRAMMAR_ID_GEN, 1), @TTL, @LVL, @BODY, @REQ)
                """,
                ct, ("@TTL", title), ("@LVL", level), ("@BODY", body), ("@REQ", FormatRequisites(requisites)));

        public Task UpdateAsync(int id, string title, string? level, string body, int[] requisites, CancellationToken ct = default) =>
            db.ExecuteAsync(
                "UPDATE GRAMMAR SET GRM_TITLE = @TTL, GRM_LEVEL = @LVL, GRM_BODY = @BODY, GRM_REQUISITES = @REQ WHERE GRM_ID = @ID",
                ct, ("@ID", id), ("@TTL", title), ("@LVL", level), ("@BODY", body), ("@REQ", FormatRequisites(requisites)));

        /// <summary>
        /// "001;002;010" (formato do AddGrammar original; o UpdateGrammar gravava sem zeros à esquerda —
        /// os dois formatos são lidos igualmente). A ordem do mapa usa GRM_REQUISITES, por isso o padding.
        /// </summary>
        public static string? FormatRequisites(int[] requisites) =>
            requisites.Length > 0 ? string.Join(";", requisites.Select(r => r.ToString("000"))) : null;

        private static TGrammar Map(DbDataReader r)
        {
            var raw = r.GetNullableString("GRM_REQUISITES");
            return new TGrammar
            {
                Index = r.GetInt("GRM_ID"),
                Title = r.GetNullableString("GRM_TITLE") ?? string.Empty,
                // Como no original: nível nulo é tratado como N5.
                Level = JlptLevel.Parse(r.GetNullableString("GRM_LEVEL"), EJLPT.N5),
                Body = r.GetNullableString("GRM_BODY") ?? string.Empty,
                RawRequisites = raw,
                Requisites = AdaptiveMap.ParseRequisites(raw).ToArray(),
            };
        }
    }

    /// <summary>Port de GrammarManager.cs + GrammarController (mapa adaptativo).</summary>
    public sealed class GrammarService(IGrammarRepository repository, IKnowledgeStore knowledge, TimeProvider? clock = null)
    {
        /// <summary>GrammarManager.ListGrammars + ordenação do GrammarController.Index.</summary>
        public async Task<IReadOnlyList<GrammarMapGroup>> GetMapAsync(int userId, EJLPT userLevel, CancellationToken ct = default) =>
            BuildMap(
                await repository.ListAsync(ct),
                await knowledge.GetDeclaredAsync(KnowledgeTables.Grammar, userId, ct),
                await repository.GetGrammarsWithExercisesAsync(ct),
                userLevel);

        public static IReadOnlyList<GrammarMapGroup> BuildMap(IEnumerable<TGrammar> grammars, IReadOnlySet<int> declared, IReadOnlySet<int> withExercises, EJLPT userLevel)
        {
            var items = grammars.Select(g =>
            {
                // COUNT(*) em GRA_MONITOR ... IN (requisitos): um registro por conteúdo declarado.
                var declaredCount = AdaptiveMap.ParseRequisites(g.RawRequisites).Distinct().Count(declared.Contains);
                var unlocked = AdaptiveMap.IsUnlocked(userLevel, g.Level, AdaptiveMap.HasKnowledge(g.RawRequisites, declaredCount));
                return new GrammarMapItem(g.Index, g.Title, g.Level, unlocked, withExercises.Contains(g.Index));
            });

            // GroupBy por nível (ordem de aparição), liberados primeiro (OrderByDescending é estável),
            // e os grupos ordenados pelo nível.
            return items
                .GroupBy(i => i.Level)
                .Select(g => new GrammarMapGroup(g.Key, g.OrderByDescending(i => i.HasEnoughLevel).ToList()))
                .OrderBy(g => g.Level)
                .ToList();
        }

        public Task<TGrammar?> GetAsync(int id, CancellationToken ct = default) => repository.GetAsync(id, ct);

        public Task<IReadOnlyList<GrammarSummary>> ListByLevelAsync(EJLPT? level, CancellationToken ct = default) =>
            repository.ListByLevelAsync(level, ct);

        public Task DeclareKnowledgeAsync(int userId, int grammarId, CancellationToken ct = default) =>
            knowledge.DeclareAsync(KnowledgeTables.Grammar, userId, grammarId, (clock ?? TimeProvider.System).GetLocalNow().DateTime, ct);

        /// <summary>GrammarController.GetRequisites.</summary>
        public async Task<GrammarRequisites?> GetRequisitesAsync(int id, EJLPT userLevel, CancellationToken ct = default)
        {
            var grammar = await repository.GetAsync(id, ct);
            if (grammar is null)
                return null;

            if (!JlptLevel.HasEnoughLevel(userLevel, grammar.Level))
                return new GrammarRequisites(true, "Conteúdo ainda não disponível.", []);

            var titles = new List<string>();
            foreach (var requisiteId in grammar.Requisites)
                if (await repository.GetAsync(requisiteId, ct) is { } requisite)
                    titles.Add(requisite.Title);

            return new GrammarRequisites(false, "Conteúdo não disponível, para tal deve-se primeiramente estudar os seguintes conteúdos:", titles);
        }

        /// <summary>GrammarController.AddGrammar (cadastro e alteração).</summary>
        public async Task<Response> SaveAsync(GrammarForm form, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(form.Title))
                return Response.Fail("É obrigatório que informe um título para o conteúdo a ser cadastrado.");
            if (string.IsNullOrWhiteSpace(form.Body))
                return Response.Fail("O conteúdo a ser cadastrado não possui um corpo.");

            var (title, level, body) = (form.Title.Trim(), JlptLevel.Normalize(form.Level), form.Body);
            var requisites = (form.Requisites ?? []).Where(r => r > 0 && r != form.Id).Distinct().ToArray();

            return await Response.SaveAsync("Conteúdo", form.Id,
                () => repository.AddAsync(title, level, body, requisites, ct),
                async () => await repository.GetAsync(form.Id, ct) is not null,
                () => repository.UpdateAsync(form.Id, title, level, body, requisites, ct));
        }
    }
}
