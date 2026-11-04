using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;

namespace Nienow.Shinrikai.Text
{
    /// <summary>Port de TText.cs.</summary>
    public class TText
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public EJLPT Level { get; set; } = EJLPT.None;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>Item da lista de textos (GText).</summary>
    public record TextListItem(int Id, string Title, EJLPT Level, bool HasEnoughLevel, bool HasExercises);

    public record TextListGroup(EJLPT Level, IReadOnlyList<TextListItem> Items);

    public sealed class TextForm
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Level { get; set; }
        public string? Body { get; set; }
    }

    public interface ITextRepository
    {
        Task<IReadOnlyList<TText>> ListAsync(CancellationToken ct = default);
        Task<TText?> GetAsync(int id, CancellationToken ct = default);
        Task<IReadOnlySet<int>> GetTextsWithExercisesAsync(CancellationToken ct = default);
        Task AddAsync(string title, string? level, string body, CancellationToken ct = default);
        Task UpdateAsync(int id, string title, string? level, string body, CancellationToken ct = default);
    }

    /// <summary>Port de DBText.cs (SQL parametrizado).</summary>
    public sealed class TextRepository(IShinrikaiDb db) : ITextRepository
    {
        public Task<IReadOnlyList<TText>> ListAsync(CancellationToken ct = default) => db.QueryAsync("SELECT * FROM TEXTS", Map, ct);

        public Task<TText?> GetAsync(int id, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync("SELECT * FROM TEXTS WHERE TXT_ID = @ID", Map, ct, ("@ID", id));

        public async Task<IReadOnlySet<int>> GetTextsWithExercisesAsync(CancellationToken ct = default) =>
            (await db.QueryAsync("SELECT DISTINCT EXR_TXT_ID FROM EXERCISES WHERE EXR_TXT_ID IS NOT NULL", r => r.GetInt("EXR_TXT_ID"), ct)).ToHashSet();

        public Task AddAsync(string title, string? level, string body, CancellationToken ct = default) =>
            db.ExecuteAsync(
                "INSERT INTO TEXTS (TXT_ID, TXT_TITLE, TXT_LEVEL, TXT_BODY) VALUES (GEN_ID(TEXTS_ID_GEN, 1), @TTL, @LVL, @BODY)",
                ct, ("@TTL", title), ("@LVL", level), ("@BODY", body));

        public Task UpdateAsync(int id, string title, string? level, string body, CancellationToken ct = default) =>
            db.ExecuteAsync(
                "UPDATE TEXTS SET TXT_TITLE = @TTL, TXT_LEVEL = @LVL, TXT_BODY = @BODY WHERE TXT_ID = @ID",
                ct, ("@ID", id), ("@TTL", title), ("@LVL", level), ("@BODY", body));

        private static TText Map(DbDataReader r) => new()
        {
            Index = r.GetInt("TXT_ID"),
            Title = r.GetNullableString("TXT_TITLE") ?? string.Empty,
            Level = JlptLevel.Parse(r.GetNullableString("TXT_LEVEL")),
            Body = r.GetNullableString("TXT_BODY") ?? string.Empty,
        };
    }

    /// <summary>Port de TextManager.cs + TextController (leitura com stretchtext e cadastro).</summary>
    public sealed class TextService(ITextRepository repository, IKnowledgeStore knowledge, IMonitorStore monitors, TimeProvider? clock = null)
    {
        /// <summary>TextManager.ListTexts + ordenação do TextController.Index.</summary>
        public async Task<IReadOnlyList<TextListGroup>> ListAsync(EJLPT userLevel, CancellationToken ct = default) =>
            BuildList(await repository.ListAsync(ct), await repository.GetTextsWithExercisesAsync(ct), userLevel);

        public static IReadOnlyList<TextListGroup> BuildList(IEnumerable<TText> texts, IReadOnlySet<int> withExercises, EJLPT userLevel) =>
            texts
                .Select(t =>
                {
                    // Como no original: texto sem nível entra no grupo N5.
                    var level = t.Level == EJLPT.None ? EJLPT.N5 : t.Level;
                    return new TextListItem(t.Index, t.Title, level, JlptLevel.HasEnoughLevel(userLevel, level), withExercises.Contains(t.Index));
                })
                .GroupBy(i => i.Level)
                .Select(g => new TextListGroup(g.Key, g.OrderByDescending(i => i.HasEnoughLevel).ToList()))
                .OrderBy(g => g.Level)
                .ToList();

        /// <summary>
        /// TextController.GetText para o aluno: converte a notação 漢字「かんじ」 e esconde a furigana
        /// das palavras ligadas ao vocabulário que ele já conhece ou nunca clicou (stretchtext).
        /// </summary>
        public async Task<TText?> GetForReadingAsync(int id, int userId, CancellationToken ct = default)
        {
            var text = await GetForEditingAsync(id, ct);
            if (text is null)
                return null;

            var counters = await monitors.GetCountersAsync(MonitorTables.Transcription, userId, Stretchtext.ReferencedWordIds(text.Body), ct);
            text.Body = Stretchtext.ApplyKnowledge(text.Body,
                wordId => Stretchtext.ShouldHideFurigana(counters.TryGetValue(wordId, out var c) ? c : null));
            return text;
        }

        /// <summary>TextManager.GetText: só a conversão da notação (usado pelo cadastro).</summary>
        public async Task<TText?> GetForEditingAsync(int id, CancellationToken ct = default)
        {
            var text = await repository.GetAsync(id, ct);
            if (text is not null)
                text.Body = Stretchtext.ConvertFuriganaNotation(text.Body);
            return text;
        }

        public Task DeclareKnowledgeAsync(int userId, int textId, CancellationToken ct = default) =>
            knowledge.DeclareAsync(KnowledgeTables.Text, userId, textId, (clock ?? TimeProvider.System).GetLocalNow().DateTime, ct);

        /// <summary>TextManager.RegisterLackOfKnowledge — o aluno clicou para ver a furigana.</summary>
        public Task RegisterLackOfKnowledgeAsync(int userId, int wordId, CancellationToken ct = default) =>
            monitors.UpdateAsync(MonitorTables.Transcription, userId, wordId, m => FragmentVariation.RegisterLackOfKnowledge(m), ct);

        /// <summary>TextManager.RemoveInvalidElements.</summary>
        public static string RemoveInvalidElements(string text) =>
            text.Replace("<ruby></ruby>", "")
                .Replace("<ruby><rb></rb></ruby>", "")
                .Replace("<ruby><rt></rt></ruby>", "");

        /// <summary>TextController.AddText (cadastro e alteração).</summary>
        public async Task<Response> SaveAsync(TextForm form, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(form.Title))
                return Response.Fail("É obrigatório que informe um título para o texto a ser cadastrado.");
            if (string.IsNullOrWhiteSpace(form.Body))
                return Response.Fail("O texto a ser cadastrado não possui um corpo.");

            var (title, level, body) = (form.Title.Trim(), JlptLevel.Normalize(form.Level), RemoveInvalidElements(form.Body));
            return await Response.SaveAsync("Texto", form.Id,
                () => repository.AddAsync(title, level, body, ct),
                async () => await repository.GetAsync(form.Id, ct) is not null,
                () => repository.UpdateAsync(form.Id, title, level, body, ct));
        }
    }
}
